package io.github.markusa380.kessleractserver

import scalatags.Text.all._

object HtmlReport {
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
