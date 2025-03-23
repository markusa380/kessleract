package io.github.markusa380.kessleractserver

import io.circe.Json
import cats.effect.IO
import cats.effect.kernel.Ref
import java.io.File
import java.nio.file.Files
import java.nio.charset.StandardCharsets
import java.nio.file.StandardOpenOption
import cats.effect.std.MapRef
import cats.syntax.all._
import scala.util.Random
import scala.io.Source
import scala.io.Codec

case class PerBodyData(
    knownIds: Ref[IO, Set[String]],
    data: MapRef[IO, String, Option[Json]]
) {
  def insert(id: String, json: Json): IO[Unit] =
    for
      _ <- data(id).update(existing => existing.getOrElse(json).some)
      _ <- knownIds.update(_ + id)
    yield ()

  def getRandomN(n: Int, excludedIds: Set[String]): IO[Map[String, Json]] =
    for
      ids <- knownIds.get
      possibleIds = ids.diff(excludedIds)
      randomIds = Random.shuffle(possibleIds.iterator).take(n)
      data <- randomIds.toList.traverse(id => data(id).get.map(_.map(id -> _)))
    yield data.flatten.toMap
}

object PerBodyData:
  def make: IO[PerBodyData] =
    for
      knownIds <- Ref.of[IO, Set[String]](Set.empty)
      data <- MapRef.ofConcurrentHashMap[IO, String, Json]()
    yield PerBodyData(knownIds, data)

class VesselDatabase(
    directory: File,
    inMemory: MapRef[IO, Int, Option[PerBodyData]]
):
  def save(body: Int, id: String, data: Json): IO[Unit] =
    val targetDir = new File(directory, body.toString)
    val tempFile = new File(targetDir, s"$id.json.tmp")
    val file = new File(targetDir, s"$id.json")

    for
      _ <- IO {
        targetDir.mkdirs()

        if (!file.exists()) {

          Files.writeString(
            tempFile.toPath,
            data.spaces4,
            StandardCharsets.UTF_8,
            StandardOpenOption.WRITE,
            StandardOpenOption.CREATE,
            StandardOpenOption.TRUNCATE_EXISTING
          )

          val _ = tempFile.renameTo(file)
        }
      }
      newBodyData <- PerBodyData.make
      forBody <- inMemory(body)
        .updateAndGet(_.getOrElse(newBodyData).some)
        .map(_.get)
      _ <- forBody.insert(id, data)
    yield ()

  def getRandomN(body: Int, n: Int, excludedIds: Set[String]): IO[Map[String, Json]] =
    for {
      forBody <- inMemory(body).get
      data <- forBody.traverse(_.getRandomN(n, excludedIds))
    } yield data.getOrElse(Map.empty)

object VesselDatabase:
  def make: IO[VesselDatabase] = for {
    inMemory <- MapRef.ofConcurrentHashMap[IO, Int, PerBodyData]()
    directory = new File("./vessel-data")
    bodyIds <- IO {
      Option(directory.listFiles()).map(_.flatMap(_.getName.toIntOption).toList).getOrElse(Nil)
    }
    _ <- bodyIds.traverse { body =>
      for
        perBodyData <- PerBodyData.make
        _ <- inMemory(body).set(perBodyData.some)
        ids <- IO {
          new File(directory, body.toString)
            .listFiles()
            .filter(_.getName.endsWith(".json"))
            .map(_.getName.stripSuffix(".json"))
            .toList
        }
        _ <- ids.traverse { id =>
          for
            json <- IO {
              val file = new File(directory, s"$body/$id.json")
              if (!file.exists()) None
              else
                io.circe.parser
                  .parse(Source.fromFile(file)(Codec.UTF8).mkString)
                  .toOption
            }
            _ <- json.traverse { json =>
              perBodyData.insert(id, json)
            }
          yield ()
        }
      yield ()
    }
  } yield VesselDatabase(directory, inMemory)
