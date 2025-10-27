package io.github.markusa380.kessleractserver

import cats.effect.IO
import com.google.protobuf.GeneratedMessage
import com.google.protobuf.util.JsonFormat
import io.circe._
import io.github.markusa380.kessleractserver.model._
import kessleract.pb.Service._
import org.http4s._
import org.http4s.dsl.io._

import org.typelevel.ci.CIStringSyntax
import scala.jdk.CollectionConverters._

class Routes(vesselRepo: VesselRepository):
  implicit val voteRequestDecoder: EntityDecoder[IO, VoteRequest] =
    EntityDecoder[IO, String].map(raw =>
      val builder = VoteRequest.newBuilder()
      JsonFormat.parser().merge(raw, builder)
      builder.build()
    )
  def vote(request: VoteRequest, ip: String): IO[Unit] =
    val vesselHash = request.getVesselHash()
    val body = request.getBody()
    val voteValue = if request.getUpvote() then 1 else -1
    vesselRepo.upsertVote(ip, vesselHash, body, voteValue)

  def downloadWithVotes(request: DownloadRequest): IO[DownloadResponse] =
    for
      vesselsWithVotes <- vesselRepo.getVesselsWithVotes(
        request.getBody(),
        request.getExcludedHashesList().asScala.toList.map(_.toInt),
        request.getTake()
      )
      uniqueVessels = vesselsWithVotes
        .map { case (hash, vessel, score) =>
          UniqueVesselSpec.newBuilder().setHash(hash).setVessel(vessel).build()
        }
        .take(Math.min(request.getTake, 10))
    yield DownloadResponse
      .newBuilder()
      .addAllVessels(uniqueVessels.asJava)
      .build()

  def upload(request: UploadRequest): IO[Unit] = {
    val hash = vesselHash(request.getVessel())
    val body = request.getBody()
    vesselRepo.upsertVessel(body, hash, request.getVessel())
  }

  val routes: HttpRoutes[IO] =
    HttpRoutes.of[IO] {
      case req @ POST -> Root / "download" =>
        val ip = getIp(req)
        println(s"Received download request from IP: $ip")
        for
          req <- req
            .as[DownloadRequest]
            .onError { case e => logDecodingErrors(e) }
          resp <- downloadWithVotes(req)
            .onError { case e => IO(println(s"Error while processing download request: $e")) }
          resp <- Ok(resp)
        yield resp
      case req @ POST -> Root / "upload" =>
        for {
          req <- req
            .as[UploadRequest]
            .onError { case e => logDecodingErrors(e) }
          _ <- upload(req)
            .onError { case e => IO(println(s"Error while processing upload request: $e")) }
          resp <- Ok()
        } yield resp
      case req @ POST -> Root / "vote" =>
        val ip = getIp(req).getOrElse("unknown")
        for {
          voteReq <- req
            .as[VoteRequest]
            .onError { case e => logDecodingErrors(e) }
          _ <- vote(voteReq, ip)
            .onError { case e => IO(println(s"Error while processing vote request: $e")) }
          resp <- Ok()
        } yield resp
    }

  def logDecodingErrors(error: Throwable): IO[Unit] = error match {
    case _: DecodingFailure => IO(println(s"Error while decoding request: $error"))
    case _                  => IO.unit
  }

  def getIp(request: Request[IO]) = request.headers.get(ci"X-Forwarded-For")
    .map(_.head.value)

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
