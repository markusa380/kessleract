package io.github.markusa380.kessleractserver

import cats.effect.IO
import com.google.protobuf.GeneratedMessage
import com.google.protobuf.util.JsonFormat
import io.circe._
import io.github.markusa380.kessleractserver.model._
import kessleract.pb.Service._
import org.http4s._
import org.http4s.dsl.io._

import scala.jdk.CollectionConverters._

class Routes(vesselRepo: VesselRepository):

  def download(request: DownloadRequest): IO[DownloadResponse] = for {
    vessels <- vesselRepo.getVessels(
      request.getBody(),
      request.getExcludedHashesList().asScala.toList.map(_.toInt),
      request.getTake()
    )
    uniqueVessels = vessels.map { case (hash, vessel) =>
      UniqueVesselSpec.newBuilder().setHash(hash).setVessel(vessel).build()
    }
  } yield DownloadResponse.newBuilder().addAllVessels(uniqueVessels.asJava).build()

  def upload(request: UploadRequest): IO[Unit] = {
    val hash = vesselHash(request.getVessel())
    val body = request.getBody()
    vesselRepo.upsertVessel(body, hash, request.getVessel())
  }

  val routes: HttpRoutes[IO] =
    HttpRoutes.of[IO] {
      case req @ POST -> Root / "download" =>
        for {
          req <- req
            .as[DownloadRequest]
            .onError { case e => logDecodingErrors(e) }
          resp <- download(req)
            .onError { case e => IO(println(s"Error while processing download request: $e")) }
          resp <- Ok(resp)
        } yield resp
      case req @ POST -> Root / "upload" =>
        for {
          req <- req
            .as[UploadRequest]
            .onError { case e => logDecodingErrors(e) }
          _ <- upload(req)
            .onError { case e => IO(println(s"Error while processing upload request: $e")) }
          resp <- Ok()
        } yield resp
    }

  def logDecodingErrors(error: Throwable): IO[Unit] = error match {
    case _: DecodingFailure => IO(println(s"Error while decoding request: $error"))
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
