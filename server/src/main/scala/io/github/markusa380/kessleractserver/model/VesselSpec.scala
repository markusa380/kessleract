package io.github.markusa380.kessleractserver.model

import io.circe._
import io.circe.generic.semiauto._

final case class VesselSpec(
    orbit: OrbitSpec,
    parts: List[PartSpec],
) {
  // To deduplicate vessels, while being lenient with positions, order of parts, etc.
  def vesselHash: Int   = parts.map(_.name).sorted.hashCode
  def validate: Boolean = true // TODO
}

object VesselSpec {
  implicit val encoder: Encoder[VesselSpec] = deriveEncoder[VesselSpec]
  implicit val decoder: Decoder[VesselSpec] = deriveDecoder[VesselSpec]
}

// This does not include the reference body index,
// as that one is provided externally
final case class OrbitSpec(
    semiMajorAxis: Double,
    eccentricity: Double,
    inclination: Double,
    argumentOfPeriapsis: Double,
    longitudeOfAscendingNode: Double,
    meanAnomalyAtEpoch: Double,
    epoch: Double
)

object OrbitSpec {
  implicit val encoder: Encoder[OrbitSpec] = deriveEncoder[OrbitSpec]
  implicit val decoder: Decoder[OrbitSpec] = deriveDecoder[OrbitSpec]
}

final case class PartSpec(
    name: String,
    position: Vector3,
    rotation: Quaternion,
    attachments: List[String],
    parentIndex: Int,
)

object PartSpec {
  implicit val encoder: Encoder[PartSpec] = deriveEncoder[PartSpec]
  implicit val decoder: Decoder[PartSpec] = deriveDecoder[PartSpec]
}

final case class Vector3(
    x: Double,
    y: Double,
    z: Double
)

object Vector3 {
  implicit val encoder: Encoder[Vector3] = deriveEncoder[Vector3]
  implicit val decoder: Decoder[Vector3] = deriveDecoder[Vector3]
}

final case class Quaternion(
    x: Double,
    y: Double,
    z: Double,
    w: Double
)

object Quaternion {
  implicit val encoder: Encoder[Quaternion] = deriveEncoder[Quaternion]
  implicit val decoder: Decoder[Quaternion] = deriveDecoder[Quaternion]
}
