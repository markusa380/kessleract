package io.github.markusa380.kessleractserver

import cats.effect.IO
import org.http4s.dsl.io._

import io.circe.generic.semiauto._
import io.circe._
import org.http4s._
import org.http4s.circe._

import io.github.markusa380.kessleractserver.model.VesselSpec
import io.github.markusa380.kessleractserver.model.VesselCollection
import cats.effect.kernel.Ref

class Routes(vesselDatabase: Ref[IO, VesselCollection]):

  def download(request: DownloadRequest): IO[DownloadResponse] = for {
    vesselsCollection <- vesselDatabase.get
    vesselsPerBody = request.bodies.toList.map { case (body, take) =>
      val vessels = vesselsCollection.getOrElse(body, Map.empty)
      body ->
        vessels.iterator
          .filter { case (hash, _) => !request.excludedHashes.contains(hash) }
          .map { case (hash, vessel) => UniqueVessel(hash, vessel) }
          .take(take)
          .toList
    }.toMap
  } yield DownloadResponse(vesselsPerBody)

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
  case class DownloadRequest(bodies: Map[Int, Int], excludedHashes: Set[Int])
  case class DownloadResponse(bodies: Map[Int, List[UniqueVessel]])
  case class UniqueVessel(hash: Int, vessel: VesselSpec)

  implicit val uploadRequestDecoder: Decoder[UploadRequest]                       = deriveDecoder[UploadRequest]
  implicit val downloadRequestDecoder: Decoder[DownloadRequest]                   = deriveDecoder[DownloadRequest]
  implicit val downloadResponseEncoder: Encoder[DownloadResponse]                 = deriveEncoder[DownloadResponse]
  implicit val uniqueVesselEncoder: Encoder[UniqueVessel]                         = deriveEncoder[UniqueVessel]
  implicit val uploadRequestEntityDecoder: EntityDecoder[IO, UploadRequest]       = jsonOf[IO, UploadRequest]
  implicit val downloadRequestEntityDecoder: EntityDecoder[IO, DownloadRequest]   = jsonOf[IO, DownloadRequest]
  implicit val downloadResponseEntityEncoder: EntityEncoder[IO, DownloadResponse] = jsonEncoderOf[IO, DownloadResponse]
