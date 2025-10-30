package io.github.markusa380.kessleractserver

import cats.effect._
import io.github.markusa380.kessleractserver.model.VesselRepository
import org.http4s._
import org.http4s.dsl.io._
import org.typelevel.ci.CIStringSyntax
import scalatags.Text.all._

case class HtmlReport(vesselRepo: VesselRepository) {

  val routes: HttpRoutes[IO] =
    HttpRoutes.of[IO] { case GET -> Root =>
      for {
        html <- htmlReport
        resp <- Ok(html, Headers(Header.Raw(ci"Content-Type", "text/html; charset=UTF-8")))
      } yield resp
    }

  def htmlReport: IO[String] =
    for
      vehicleCount <- vesselRepo.getVehicleCount
      votes        <- vesselRepo.getVoteCounts
    yield render(vehicleCount, votes._1, votes._2)

  def render(vehicleCount: Int, upvotes: Int, downvotes: Int): String = {
    html(
      head(
        title := "Kessleract Report",
        meta(charset := "UTF-8")
      ),
      body(
        h1("Kessleract Report"),
        ul(
          li(s"Number of vehicles: $vehicleCount"),
          li(s"Upvotes cast: $upvotes"),
          li(s"Downvotes cast: $downvotes")
        )
      )
    ).render
  }
}
