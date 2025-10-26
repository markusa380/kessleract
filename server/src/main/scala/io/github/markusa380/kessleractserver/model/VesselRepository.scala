package io.github.markusa380.kessleractserver.model

import cats.effect.IO
import doobie._
import doobie.hikari.HikariTransactor
import doobie.implicits._
import kessleract.pb.Messages._

class VesselRepository(transactor: HikariTransactor[IO]) {
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
        LIMIT $take
      """
    Fragment.const(query).query[(Int, Array[Byte])].to[List].transact(transactor).map { rows =>
      rows.map { case (hash, bytes) =>
        (hash, VesselSpec.newBuilder().mergeFrom(bytes).build())
      }
    }
  }
}
