package io.github.markusa380.kessleractserver.model

import cats.effect._
import io.circe.parser.decode
import io.circe.syntax._

import java.io.IOException
import java.nio.charset.StandardCharsets
import java.nio.file.Files
import java.nio.file.Paths
import java.nio.file.StandardCopyOption

type HashedVesselCollection = Map[Int, Map[Int, VesselSpec]]
type VesselCollection       = Map[Int, List[VesselSpec]]

val tempFilePath = Paths.get("./vessels.json.tmp")
val filePath     = Paths.get("./vessels.json")

def persist(vessels: HashedVesselCollection): IO[Unit] = {
  val unhashed: VesselCollection =
    vessels.view.mapValues(_.values.toList).toMap

  IO {
    val _ = Files.write(
      tempFilePath,
      unhashed.asJson.spaces4.getBytes(StandardCharsets.UTF_8)
    )

    val _ = Files.move(
      tempFilePath,
      filePath,
      StandardCopyOption.REPLACE_EXISTING,
      StandardCopyOption.ATOMIC_MOVE
    )
  }
}

def load: IO[HashedVesselCollection] = {
  IO {
    if Files.exists(filePath) then
      val json =
        new String(Files.readAllBytes(filePath), StandardCharsets.UTF_8)
      decode[VesselCollection](json) match {
        case Left(errors) =>
          throw new IOException(s"Failed to decode vessels.json: $errors")
        case Right(vessels) => vessels.view.mapValues(_.map(v => v.vesselHash -> v).toMap).toMap
      }
    else Map.empty
  }
}
