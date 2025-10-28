package io.github.markusa380.kessleractserver

import scala.io.Source
import scala.jdk.CollectionConverters._

val allowedParts    = Source.fromResource("allowed_parts.txt").getLines().toSet
val vesselPartBound = 50.0
val rotationBound   = 1000.0

def validateVesselSpec(vessel: kessleract.pb.Messages.VesselSpec): Either[String, Unit] = for {
  _ <- Either.cond(vessel.getPartSpecsCount() > 0, (), "Vessel must have at least one part")
  _ <- vessel
    .getPartSpecsList()
    .asScala
    .toList
    .map(part => validatePart(part))
    .foldLeft[Either[String, Unit]](Right(())) { (acc, res) =>
      for {
        _ <- acc
        _ <- res
      } yield ()
    }
  _ <- validateOrbit(vessel.getOrbitSpec())
} yield ()

def validatePart(
    part: kessleract.pb.Messages.PartSpec
): Either[String, Unit] = for {
  _ <- Either.cond(allowedParts.contains(part.getName()), (), "Part name cannot be empty")
  _ <- validatePartPosition(part.getPosition())
  _ <- validatePartRotation(part.getRotation())
  // TODO: Maybe also validate the attachments
} yield ()

def validatePartPosition(position: kessleract.pb.Messages.Vector3): Either[String, Unit] = for {
  _ <- Either.cond(position.getX() >= -vesselPartBound && position.getX() <= vesselPartBound, (), s"Part position X out of bounds: ${position.getX()}")
  _ <- Either.cond(position.getY() >= -vesselPartBound && position.getY() <= vesselPartBound, (), s"Part position Y out of bounds: ${position.getY()}")
  _ <- Either.cond(position.getZ() >= -vesselPartBound && position.getZ() <= vesselPartBound, (), s"Part position Z out of bounds: ${position.getZ()}")
} yield ()

def validatePartRotation(rotation: kessleract.pb.Messages.Quaternion): Either[String, Unit] = for {
  _ <- Either.cond(rotation.getX() >= -rotationBound && rotation.getX() <= rotationBound, (), s"Part rotation X out of bounds: ${rotation.getX()}")
  _ <- Either.cond(rotation.getY() >= -rotationBound && rotation.getY() <= rotationBound, (), s"Part rotation Y out of bounds: ${rotation.getY()}")
  _ <- Either.cond(rotation.getZ() >= -rotationBound && rotation.getZ() <= rotationBound, (), s"Part rotation Z out of bounds: ${rotation.getZ()}")
  _ <- Either.cond(rotation.getW() >= -rotationBound && rotation.getW() <= rotationBound, (), s"Part rotation W out of bounds: ${rotation.getW()}")
} yield ()

def validateOrbit(orbit: kessleract.pb.Messages.OrbitSpec): Either[String, Unit] = for {
  _ <- Either.cond(orbit.getSemiMajorAxis() > 0, (), "Orbit semi-major axis must be positive")
  _ <- Either.cond(orbit.getEccentricity() >= 0 && orbit.getEccentricity() < 1, (), "Orbit eccentricity must be in [0, 1)")
  _ <- Either.cond(orbit.getInclination() >= 0 && orbit.getInclination() <= 180, (), "Orbit inclination must be in [0, 180]")
  _ <- Either.cond(orbit.getArgumentOfPeriapsis() >= 0 && orbit.getArgumentOfPeriapsis() < 360, (), "Orbit argument of periapsis must be in [0, 360)")
  _ <- Either.cond(orbit.getLongitudeOfAscendingNode() >= 0 && orbit.getLongitudeOfAscendingNode() < 360, (), "Orbit longitude of ascending node must be in [0, 360)")
  _ <- Either.cond(orbit.getMeanAnomalyAtEpoch() >= 0 && orbit.getMeanAnomalyAtEpoch() < 360, (), "Orbit mean anomaly at epoch must be in [0, 360)")
} yield ()

def validateBody(body: Int): Either[String, Unit] =
  val validBodies = Set.range(0, 17)
  if validBodies.contains(body) then Right(())
  else Left(s"Invalid body ID: $body")
