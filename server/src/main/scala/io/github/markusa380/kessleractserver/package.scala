package io.github.markusa380

import cats.effect.IO
import kessleract.pb.messages.VesselSpec
import org.typelevel.log4cats.slf4j.Slf4jLogger

package object kessleractserver {
  def vesselHash(vessel: VesselSpec): Int =
    vessel.partSpecs.map(_.name).sorted.hashCode

  val log = Slf4jLogger.getLogger[IO]
}
