package io.github.markusa380.kessleractserver

import cats.effect.IO
import com.comcast.ip4s._
import io.github.markusa380.kessleractserver.model._
import org.http4s.ember.client.EmberClientBuilder
import org.http4s.ember.server.EmberServerBuilder
import org.http4s.implicits._
import org.http4s.server.middleware.EntityLimiter
import org.http4s.server.middleware.Logger

object Server:

  lazy val port = sys.env.get("PORT").getOrElse("8080").toInt

  def run: IO[Nothing] =
    (for {
      client <- EmberClientBuilder.default[IO].build
      tx     <- Database.transactor
      vesselDatabase = VesselRepository(tx)
      httpApp        = Routes(vesselDatabase).routes.orNotFound

      // With Middlewares in place
      finalHttpApp = Logger.httpApp(true, true)(
        EntityLimiter(httpApp, 100_000)
      )

      _ <-
        EmberServerBuilder
          .default[IO]
          .withHost(ipv4"0.0.0.0")
          .withPort(Port.fromInt(port).get)
          .withHttpApp(finalHttpApp)
          .build
    } yield ()).useForever
