package io.github.markusa380.kessleractserver.model

import cats.effect._
import io.circe.syntax._
import io.circe.parser.decode
import java.nio.file.{Files, Paths, StandardCopyOption}
import java.nio.charset.StandardCharsets
import java.io.IOException

type VesselCollection = Map[Int, Map[Int, VesselSpec]]

val tempFilePath = Paths.get("./vessels.json.tmp")
val filePath     = Paths.get("./vessels.json")

def persist(vessels: VesselCollection): IO[Unit] = {
  val json = vessels.asJson

  IO {
    val _ = Files.write(
      tempFilePath,
      json.spaces4.getBytes(StandardCharsets.UTF_8)
    )

    val _ = Files.move(
      tempFilePath,
      filePath,
      StandardCopyOption.REPLACE_EXISTING,
      StandardCopyOption.ATOMIC_MOVE
    )
  }
}

def load: IO[VesselCollection] = {
  IO {
    if Files.exists(filePath) then
      val json =
        new String(Files.readAllBytes(filePath), StandardCharsets.UTF_8)
      decode[VesselCollection](json) match {
        case Left(errors) =>
          throw new IOException(s"Failed to decode vessels.json: $errors")
        case Right(vessels) => vessels
      }
    else Map.empty
  }
}
