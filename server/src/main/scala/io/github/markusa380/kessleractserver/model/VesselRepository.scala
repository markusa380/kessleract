package io.github.markusa380.kessleractserver.model

import cats.data.NonEmptySeq
import cats.effect.IO
import doobie._
import doobie.hikari.HikariTransactor
import doobie.implicits._
import doobie.postgres.implicits._
import kessleract.pb.messages._

class VesselRepository(transactor: HikariTransactor[IO]) {

  def alreadyExists(bodyId: Int, vessel: VesselSpec): IO[Boolean] = {
    val vesselHash = vessel.partSpecs.map(_.name).sorted.hashCode
    sql"""
      SELECT COUNT(*) FROM vessel WHERE body_id = $bodyId AND vessel_hash = $vesselHash
    """.query[Int].unique.transact(transactor).map(_ > 0)
  }

  def getVotes(bodyId: Int, vessel: VesselSpec): IO[Int] = {
    val vesselHash = vessel.partSpecs.map(_.name).sorted.hashCode
    sql"""
      SELECT COALESCE(SUM(vote), 0) FROM vessel_votes WHERE body = $bodyId AND vessel_hash = $vesselHash
    """.query[Int].unique.transact(transactor)
  }

  // Upsert a vote for a vessel
  def upsertVote(ip: String, vesselHash: Int, body: Int, vote: Int): IO[Unit] = {
    sql"""
      INSERT INTO vessel_votes (ip, vessel_hash, body, vote)
      VALUES ($ip, $vesselHash, $body, $vote)
      ON CONFLICT (ip, vessel_hash, body)
      DO UPDATE SET vote = EXCLUDED.vote
    """.update.run.transact(transactor).void
  }

  // Get vessels for a body, excluding hashes, ordered by vote score
  def getVesselsWithVotes(
      bodyId: Int,
      excludedHashes: Seq[Int],
      take: Int,
      allowablePartsOpt: Option[Seq[String]]
  ): IO[List[(Int, VesselSpec, Int)]] = {
    val exclusion = NonEmptySeq.fromSeq(excludedHashes).fold(fr0"")(neExcludedHashes => fr"AND NOT ${Fragments.in(fr"v.vessel_hash", neExcludedHashes)}")
    val partsFilter = allowablePartsOpt match {
      // For backwards compatibility, also allow vessels with NULL parts
      case Some(allowableParts) => fr"AND (parts IS NULL OR parts <@ ${allowableParts.toList}::text[])"
      case None                 => fr0""
    }
    sql"""
        SELECT v.vessel_hash, v.vessel_spec, COALESCE(vv.score, 0) AS score
        FROM vessel v
        LEFT JOIN (
          SELECT vessel_hash, body, SUM(vote) AS score
          FROM vessel_votes
          GROUP BY vessel_hash, body
        ) vv ON v.vessel_hash = vv.vessel_hash AND v.body_id = vv.body
        WHERE v.body_id = $bodyId $exclusion $partsFilter
        ORDER BY (COALESCE(vv.score, 0) * random()) DESC
        LIMIT $take
      """.query[(Int, Array[Byte], Int)].to[List].transact(transactor).map { rows =>
      rows.map { case (hash, bytes, score) =>
        (hash, VesselSpec.parseFrom(bytes), score)
      }
    }
  }

  // Get the total count of vessels
  def getVehicleCount: IO[Int] = {
    sql"SELECT COUNT(*) FROM vessel".query[Int].unique.transact(transactor)
  }

  // Get total upvotes and downvotes
  def getVoteCounts: IO[(Int, Int)] = {
    sql"""
          SELECT
            SUM(CASE WHEN vote = 1 THEN 1 ELSE 0 END) AS upvotes,
            SUM(CASE WHEN vote = -1 THEN 1 ELSE 0 END) AS downvotes
          FROM vessel_votes
        """.query[(Int, Int)].unique.transact(transactor)
  }

  // Insert or update vessel
  def upsertVessel(bodyId: Int, vesselHash: Int, vessel: VesselSpec): IO[Unit] = {
    val parts           = vessel.partSpecs.map(_.name).toList
    val vesselSpecBytes = vessel.toByteArray
    sql"""
      INSERT INTO vessel (body_id, vessel_hash, vessel_spec, parts)
      VALUES ($bodyId, $vesselHash, $vesselSpecBytes, $parts)
      ON CONFLICT (body_id, vessel_hash)
      DO UPDATE SET vessel_spec = EXCLUDED.vessel_spec
    """.update.run.transact(transactor).void
  }
}
