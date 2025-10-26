package io.github.markusa380.kessleractserver

import cats.effect.IO
import cats.effect.kernel.Ref
import io.circe._
import io.github.markusa380.kessleractserver.model._
import org.http4s._
import org.http4s.dsl.io._
import kessleract.pb.Service.*
import scala.jdk.CollectionConverters._
import com.google.protobuf.GeneratedMessage
import com.google.protobuf.util.JsonFormat

class Routes(vesselDatabase: Ref[IO, HashedVesselCollection]):

  def download(request: DownloadRequest): IO[DownloadResponse] = for {
    vesselsCollection <- vesselDatabase.get

    vessels = vesselsCollection
      .getOrElse(request.getBody(), Map.empty)
      .iterator
      .filter { case (hash, _) => !request.getExcludedHashesList().contains(hash) }
      .map { case (hash, vessel) => UniqueVesselSpec.newBuilder().setHash(hash).setVessel(vessel).build() }
      .take(request.getTake())
      .toList

  } yield DownloadResponse.newBuilder().addAllVessels(vessels.asJava).build()

  def upload(request: UploadRequest): IO[Unit] = for {
    db <- vesselDatabase.updateAndGet(collection =>
      val hash           = vesselHash(request.getVessel())
      val body           = request.getBody()
      val vessels        = collection.getOrElse(body, Map.empty)
      val updatedVessels = vessels.updated(hash, request.getVessel())
      collection.updated(body, updatedVessels)
    )
    _ <- persist(db)
  } yield ()

  val routes: HttpRoutes[IO] =
    HttpRoutes.of[IO] {
      case req @ POST -> Root / "download" =>
        for {
          req <- req
            .as[DownloadRequest]
            .onError(logDecodingErrors)
          resp <- download(req)
          resp <- Ok(resp)
        } yield resp
      case req @ POST -> Root / "upload" =>
        for {
          req <- req
            .as[UploadRequest]
            .onError(logDecodingErrors)
          resp <- upload(req)
          resp <- Ok()
        } yield resp
    }

  def logDecodingErrors(error: Throwable): IO[Unit] = error match {
    case _: DecodingFailure => IO(println(s"Error while decoding upload request: $error"))
    case _                  => IO.unit
  }

  implicit val downloadRequestDecoder: EntityDecoder[IO, DownloadRequest] =
    EntityDecoder[IO, String].map(raw =>
      val builder = DownloadRequest.newBuilder()
      JsonFormat.parser().merge(raw, builder)
      builder.build()
    )

  implicit val uploadRequestDecoder: EntityDecoder[IO, UploadRequest] =
    EntityDecoder[IO, String].map(raw =>
      val builder = UploadRequest.newBuilder()
      JsonFormat.parser().merge(raw, builder)
      builder.build()
    )

  implicit def responseEncoder[A <: GeneratedMessage]: EntityEncoder[IO, A] =
    EntityEncoder[IO, String].contramap[A](msg => JsonFormat.printer().print(msg))

  
