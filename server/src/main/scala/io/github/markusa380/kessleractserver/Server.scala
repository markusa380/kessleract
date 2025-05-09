package io.github.markusa380.kessleractserver

import cats.effect.IO
import cats.effect.kernel.Ref
import com.comcast.ip4s._
import io.github.markusa380.kessleractserver.model._
import org.http4s.ember.client.EmberClientBuilder
import org.http4s.ember.server.EmberServerBuilder
import org.http4s.implicits._
import org.http4s.server.middleware.EntityLimiter
import org.http4s.server.middleware.Logger

object Server:

  def run: IO[Nothing] =
    (for {
      client         <- EmberClientBuilder.default[IO].build
      loaded         <- load.toResource
      vesselDatabase <- Ref.of[IO, HashedVesselCollection](loaded).toResource
      httpApp = Routes(vesselDatabase).routes.orNotFound

      // With Middlewares in place
      finalHttpApp = Logger.httpApp(true, true)(
        EntityLimiter(httpApp, 100_000)
      )

      _ <-
        EmberServerBuilder
          .default[IO]
          .withHost(ipv4"0.0.0.0")
          .withPort(port"8080")
          .withHttpApp(finalHttpApp)
          .build
    } yield ()).useForever
