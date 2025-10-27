package io.github.markusa380.kessleractserver.model

import cats.effect.IO
import doobie._
import doobie.hikari.HikariTransactor
import doobie.implicits._
import kessleract.pb.Messages._

class VesselRepository(transactor: HikariTransactor[IO]) {
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
      excludedHashes: List[Int],
      take: Int
  ): IO[List[(Int, VesselSpec, Int)]] = {
    val exclusion = if (excludedHashes.isEmpty) "" else s"AND v.vessel_hash NOT IN (${excludedHashes.mkString(",")})"
    val query =
      s"""
        SELECT v.vessel_hash, v.vessel_spec, COALESCE(vv.score, 0) AS score
        FROM vessel v
        LEFT JOIN (
          SELECT vessel_hash, body, SUM(vote) AS score
          FROM vessel_votes
          GROUP BY vessel_hash, body
        ) vv ON v.vessel_hash = vv.vessel_hash AND v.body_id = vv.body
        WHERE v.body_id = $bodyId $exclusion
        ORDER BY (COALESCE(vv.score, 0) * random()) DESC
        LIMIT $take
      """
    Fragment.const(query).query[(Int, Array[Byte], Int)].to[List].transact(transactor).map { rows =>
      rows.map { case (hash, bytes, score) =>
        (hash, VesselSpec.newBuilder().mergeFrom(bytes).build(), score)
      }
    }
  }
  // Insert or update vessel
  def upsertVessel(bodyId: Int, vesselHash: Int, vessel: VesselSpec): IO[Unit] = {
    val vesselSpec = vessel.toByteArray
    sql"""
      INSERT INTO vessel (body_id, vessel_hash, vessel_spec)
      VALUES ($bodyId, $vesselHash, $vesselSpec)
      ON CONFLICT (body_id, vessel_hash)
      DO UPDATE SET vessel_spec = EXCLUDED.vessel_spec
    """.update.run.transact(transactor).void
  }

  // Get vessels for a body, excluding hashes
  def getVessels(
      bodyId: Int,
      excludedHashes: List[Int],
      take: Int
  ): IO[List[(Int, VesselSpec)]] = {
    val exclusion = if (excludedHashes.isEmpty) "" else s"AND vessel_hash NOT IN (${excludedHashes.mkString(",")})"
    val query =
      s"""
        SELECT vessel_hash, vessel_spec FROM vessel
        WHERE body_id = $bodyId $exclusion
        ORDER BY random()
        LIMIT $take
      """
    Fragment.const(query).query[(Int, Array[Byte])].to[List].transact(transactor).map { rows =>
      rows.map { case (hash, bytes) =>
        (hash, VesselSpec.newBuilder().mergeFrom(bytes).build())
      }
    }
  }
}
