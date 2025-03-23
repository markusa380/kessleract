package io.github.markusa380.kessleractserver

import cats.effect.IO
import org.http4s.dsl.io._

import io.circe.generic.semiauto._
import io.circe._
import org.http4s._
import org.http4s.circe._

import cats.syntax.all._

object Routes:

  case class UploadRequest(body: Int, id: String, data: Json)
  case class DownloadRequest(bodies: Map[Int, Int], excludedIds: Set[String])
  case class DownloadResponse(bodies: Map[Int, Map[String, Json]])

  implicit val uploadRequestDecoder: Decoder[UploadRequest] =
    deriveDecoder[UploadRequest]
  implicit val downloadRequestDecoder: Decoder[DownloadRequest] =
    deriveDecoder[DownloadRequest]
  implicit val downloadResponseEncoder: Encoder[DownloadResponse] =
    deriveEncoder[DownloadResponse]

  implicit val uploadRequestEntityDecoder: EntityDecoder[IO, UploadRequest] =
    jsonOf[IO, UploadRequest]
  implicit val downloadRequestEntityDecoder
      : EntityDecoder[IO, DownloadRequest] = jsonOf[IO, DownloadRequest]
  implicit val downloadResponseEntityEncoder
      : EntityEncoder[IO, DownloadResponse] =
    jsonEncoderOf[IO, DownloadResponse]

  def routes(
      vesselDatabase: VesselDatabase
  ): HttpRoutes[IO] =
    HttpRoutes.of[IO] {
      case req @ POST -> Root / "download" =>
        for {
          req <- req.as[DownloadRequest]
          bodies <- req.bodies.toList.traverse { case (body, n) =>
            vesselDatabase
              .getRandomN(body, n, req.excludedIds)
              .map(body -> _)
          }
          resp = DownloadResponse(bodies.toMap)
          resp <- Ok(resp)
        } yield resp
      case req @ POST -> Root / "upload" =>
        for {
          req <- req.as[UploadRequest]
          _ <- vesselDatabase.save(req.body, req.id, req.data)
          resp <- Ok()
        } yield resp
    }
