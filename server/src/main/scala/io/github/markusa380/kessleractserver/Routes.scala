package io.github.markusa380.kessleractserver

import cats.data.EitherT
import cats.effect.IO
import cats.effect.std.Random
import cats.syntax.all._
import io.github.markusa380.kessleractserver.model._
import kessleract.pb.service._
import org.http4s._
import org.http4s.circe.CirceEntityDecoder._
import org.http4s.circe.CirceEntityEncoder._
import org.http4s.dsl.io._
import org.typelevel.ci.CIStringSyntax
import scalapb_circe.codec._

class Routes(vesselRepo: VesselRepository):

  def vote(request: VoteRequest, ip: String): IO[Unit] =
    val vesselHash = request.vesselHash
    val body       = request.body
    val voteValue  = if request.upvote then 1 else -1
    vesselRepo.upsertVote(ip, vesselHash, body, voteValue)

  def downloadWithVotes(request: DownloadRequest): IO[DownloadResponse] =
    for
      vesselsWithVotes <- vesselRepo.getVesselsWithVotes(
        request.body,
        request.excludedHashes,
        request.take,
        if (request.allowableParts.isEmpty) None else request.allowableParts.some
      )
      uniqueVessels = vesselsWithVotes
        .map { case (hash, vessel, score) => UniqueVesselSpec(vessel.some, hash) }
        .take(Math.min(request.take, 10))
    yield DownloadResponse(uniqueVessels)

  def upload(request: UploadRequest): EitherT[IO, ValidationError, Unit] =
    val body = request.body
    for
      vessel <- EitherT.fromOption(request.vessel, ValidationError("Missing vessel spec"))
      hash = vesselHash(vessel)
      _ <- EitherT.fromEither(validateBody(body).left.map(error => ValidationError(error)))
      _ <- EitherT.fromEither(validateVesselSpec(request.getVessel).left.map(error => ValidationError(error)))
      _ <- EitherT.liftF(vesselRepo.upsertVessel(body, hash, vessel))
    yield ()

  val routes: HttpRoutes[IO] =
    HttpRoutes.of[IO] {
      case req @ POST -> Root / "download" =>
        logging(req)(
          for
            req  <- req.as[DownloadRequest]
            resp <- downloadWithVotes(req)
            resp <- Ok(resp)
          yield resp
        )
      case req @ POST -> Root / "upload" =>
        logging(req)(
          for {
            req <- req.as[UploadRequest]
            res <- upload(req).value
            resp <- res.fold(
              err => BadRequest(err.message),
              _ => Ok()
            )
          } yield resp
        )
      case req @ POST -> Root / "vote" =>
        logging(req)(
          for {
            voteReq <- req.as[VoteRequest]
            ip = getIp(req).getOrElse("unknown")
            _    <- vote(voteReq, ip)
            resp <- Ok()
          } yield resp
        )
    }

  def logRequest(request: Request[IO])(requestId: RequestId): IO[Unit] =
    log.info(s"${requestId} Received request: ${request.method} ${request.uri}")

  def logging(req: Request[IO])(response: IO[Response[IO]]): IO[Response[IO]] = for
    requestId <- Random[IO].nextAlphaNumeric.replicateA(8).map(_.mkString).map(RequestId.apply)
    ip = getIp(req).getOrElse("unknown")
    _ <- log.info(s"${requestId} Processing request: ${req.method} ${req.uri} (IP: $ip)")
    response <- response
      .handleErrorWith(e =>
        log.error(e)(s"${requestId} Error processing request: ${e.getMessage}") *>
          InternalServerError()
      )
    _ <- log.info(s"${requestId} Response status: ${response.status.code}")
  yield response

  def getIp(request: Request[IO]) = request.headers
    .get(ci"X-Forwarded-For")
    .map(_.head.value)

case class RequestId(value: String) extends AnyVal {
  override def toString: String = s"[$value]"
}

case class ValidationError(message: String)
