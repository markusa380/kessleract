package io.github.markusa380.kessleractserver

import kessleract.pb.{messages => pb}

val vesselPartBound         = 50.0
val rotationBound           = 1000.0
val maxVesselParts          = 200
val maxPartNameLength       = 50
val maxAttachNodeNameLength = 50

def validateVesselSpec(vessel: pb.VesselSpec): Either[String, Unit] = for {
  _ <- Either.cond(vessel.partSpecs.size > 0, (), "Vessel must have at least one part")
  _ <- Either.cond(vessel.partSpecs.size <= 200, (), s"Vessel cannot have more than $maxVesselParts parts, found: ${vessel.partSpecs.size}")
  _ <- vessel.partSpecs
    .map(validatePart)
    .foldLeft[Either[String, Unit]](Right(())) { (acc, res) =>
      for {
        _ <- acc
        _ <- res
      } yield ()
    }
  _ <- vessel.orbitSpec.fold(Left("Missing orbit"))(validateOrbit)
} yield ()

def validatePart(
    part: pb.PartSpec
): Either[String, Unit] = for {
  _ <- Either.cond(part.name.nonEmpty, (), "Part name cannot be empty")
  _ <- Either.cond(part.name.length <= maxPartNameLength, (), s"Part name exceeds maximum length of $maxPartNameLength characters")
  _ <- part.position.fold(Left("Part position cannot be empty"))(validatePartPosition)
  _ <- part.rotation.fold(Left("Part rotation cannot be empty"))(validatePartRotation)
  _ <- part.attachments.foldLeft[Either[String, Unit]](Right(())) { (acc, attachment) =>
    acc.flatMap(_ => validateAttachNode(attachment))
  }
  _ <- part.surfaceAttachment.fold(Right(()))(validateAttachNode)
} yield ()

def validateAttachNode(name: String): Either[String, Unit] = for {
  _ <- Either.cond(name.nonEmpty, (), "Attachment node name cannot be empty")
  _ <- Either.cond(name.length <= maxAttachNodeNameLength, (), s"Attachment node name exceeds maximum length of $maxAttachNodeNameLength characters")
  _ <- name match {
    case s"$id,$idx,$mesh" if idx.trim.toIntOption.isDefined => Right(())
    case s"$id,$idx" if idx.trim.toIntOption.isDefined       => Right(())
    case _                                                   => Left(s"Attachment node name has invalid format: $name")
  }

} yield ()

def validatePartPosition(position: pb.Vector3): Either[String, Unit] = for {
  _ <- Either.cond(position.x >= -vesselPartBound && position.x <= vesselPartBound, (), s"Part position X out of bounds: ${position.x}")
  _ <- Either.cond(position.y >= -vesselPartBound && position.y <= vesselPartBound, (), s"Part position Y out of bounds: ${position.y}")
  _ <- Either.cond(position.z >= -vesselPartBound && position.z <= vesselPartBound, (), s"Part position Z out of bounds: ${position.z}")
} yield ()

def validatePartRotation(rotation: pb.Quaternion): Either[String, Unit] = for {
  _ <- Either.cond(rotation.x >= -rotationBound && rotation.x <= rotationBound, (), s"Part rotation X out of bounds: ${rotation.x}")
  _ <- Either.cond(rotation.y >= -rotationBound && rotation.y <= rotationBound, (), s"Part rotation Y out of bounds: ${rotation.y}")
  _ <- Either.cond(rotation.z >= -rotationBound && rotation.z <= rotationBound, (), s"Part rotation Z out of bounds: ${rotation.z}")
  _ <- Either.cond(rotation.w >= -rotationBound && rotation.w <= rotationBound, (), s"Part rotation W out of bounds: ${rotation.w}")
} yield ()

def validateOrbit(orbit: pb.OrbitSpec): Either[String, Unit] = for {
  _ <- Either.cond(orbit.semiMajorAxis > 0, (), "Orbit semi-major axis must be positive")
  _ <- Either.cond(orbit.eccentricity >= 0 && orbit.eccentricity < 1, (), "Orbit eccentricity must be in [0, 1)")
  _ <- Either.cond(orbit.inclination >= 0 && orbit.inclination <= 180, (), "Orbit inclination must be in [0, 180]")
  _ <- Either.cond(orbit.argumentOfPeriapsis >= 0 && orbit.argumentOfPeriapsis < 360, (), "Orbit argument of periapsis must be in [0, 360)")
  _ <- Either.cond(orbit.longitudeOfAscendingNode >= 0 && orbit.longitudeOfAscendingNode < 360, (), "Orbit longitude of ascending node must be in [0, 360)")
  _ <- Either.cond(orbit.meanAnomalyAtEpoch >= 0 && orbit.meanAnomalyAtEpoch < 360, (), "Orbit mean anomaly at epoch must be in [0, 360)")
} yield ()

def validateBody(body: Int): Either[String, Unit] =
  val validBodies = Set.range(0, 17)
  if validBodies.contains(body) then Right(())
  else Left(s"Invalid body ID: $body")
