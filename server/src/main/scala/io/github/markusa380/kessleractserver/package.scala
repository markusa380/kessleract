package io.github.markusa380

import io.circe.Codec
import kessleract.pb.Messages.VesselSpec
import java.util.Base64
import scala.jdk.CollectionConverters._

package object kessleractserver {
  implicit val vesselSpecSerde: Codec[VesselSpec] = Codec.forProduct1[VesselSpec, String]("value")(
    str => VesselSpec.parseFrom(Base64.getDecoder.decode(str))
  )(
    vesselSpec => Base64.getEncoder.encodeToString(vesselSpec.toByteArray)
  )

  def vesselHash(vessel: VesselSpec): Int = {
    vessel.getPartSpecsList.asScala.map(_.getName()).sorted.hashCode
  }
}
