package io.github.markusa380.kessleractserver

import cats.effect.IO
import cats.effect.kernel.Ref
import io.circe._
import io.circe.generic.semiauto._
import io.github.markusa380.kessleractserver.model._
import org.http4s._
import org.http4s.circe._
import org.http4s.dsl.io._

class Routes(vesselDatabase: Ref[IO, HashedVesselCollection]):

  def download(request: DownloadRequest): IO[DownloadResponse] = for {
    vesselsCollection <- vesselDatabase.get

    vessels = vesselsCollection
      .getOrElse(request.body, Map.empty)
      .iterator
      .filter { case (hash, _) => !request.excludedHashes.contains(hash) }
      .map { case (hash, vessel) => UniqueVessel(hash, vessel) }
      .take(request.take)
      .toList

  } yield DownloadResponse(vessels)

  def upload(request: UploadRequest): IO[Unit] =
    vesselDatabase.update(collection =>
      val hash           = request.vessel.vesselHash
      val body           = request.body
      val vessels        = collection.getOrElse(body, Map.empty)
      val updatedVessels = vessels.updated(hash, request.vessel)
      collection.updated(body, updatedVessels)
    )

  val routes: HttpRoutes[IO] =
    HttpRoutes.of[IO] {
      case req @ POST -> Root / "download" =>
        for {
          req  <- req.as[DownloadRequest]
          resp <- download(req)
          resp <- Ok(resp)
        } yield resp
      case req @ POST -> Root / "upload" =>
        for {
          req  <- req.as[UploadRequest]
          resp <- upload(req)
          resp <- Ok()
        } yield resp
    }

  case class UploadRequest(body: Int, vessel: VesselSpec)
  case class DownloadRequest(body: Int, take: Int, excludedHashes: Set[Int])
  case class DownloadResponse(vessels: List[UniqueVessel])
  case class UniqueVessel(hash: Int, vessel: VesselSpec)

  implicit val uploadRequestDecoder: Decoder[UploadRequest]                       = deriveDecoder[UploadRequest]
  implicit val downloadRequestDecoder: Decoder[DownloadRequest]                   = deriveDecoder[DownloadRequest]
  implicit val downloadResponseEncoder: Encoder[DownloadResponse]                 = deriveEncoder[DownloadResponse]
  implicit val uniqueVesselEncoder: Encoder[UniqueVessel]                         = deriveEncoder[UniqueVessel]
  implicit val uploadRequestEntityDecoder: EntityDecoder[IO, UploadRequest]       = jsonOf[IO, UploadRequest]
  implicit val downloadRequestEntityDecoder: EntityDecoder[IO, DownloadRequest]   = jsonOf[IO, DownloadRequest]
  implicit val downloadResponseEntityEncoder: EntityEncoder[IO, DownloadResponse] = jsonEncoderOf[IO, DownloadResponse]
