package io.github.markusa380.kessleractserver.model

import cats.effect._
import com.dimafeng.testcontainers.PostgreSQLContainer
import com.dimafeng.testcontainers.munit._
import doobie.util.ExecutionContexts
import io.github.markusa380.kessleractserver.Database
import io.github.markusa380.kessleractserver.vesselHash
import kessleract.pb.messages._
import munit.CatsEffectSuite
import org.testcontainers.utility.DockerImageName

class VesselRepositorySuite extends CatsEffectSuite with TestContainerForEach {

  override val containerDef: PostgreSQLContainer.Def = PostgreSQLContainer.Def(
    dockerImageName = DockerImageName.parse("postgres:18"),
    databaseName = "test",
    username = "test",
    password = "test"
  )

  test("scenario based") {
    val bodyId = 42
    withRepo { repo =>
      for {
        exists1 <- repo.alreadyExists(bodyId, testVessel)
        _ = assert(!exists1, "Vessel should not exist yet")
        _       <- repo.upsertVessel(bodyId, testVessel)
        exists2 <- repo.alreadyExists(bodyId, testVessel)
        _    = assert(exists2, "Vessel should exist")
        hash = vesselHash(testVessel)
        r1 <- repo.getVesselsWithVotes(
          bodyId,
          excludedHashes = Seq.empty,
          take = 10,
          allowablePartsOpt = None
        )
        _ = assert(
          r1 == List((hash, testVessel, 0)),
          "Should return the inserted vessel"
        )
        r2 <- repo.getVesselsWithVotes(
          bodyId,
          excludedHashes = Seq(hash),
          take = 1,
          allowablePartsOpt = None
        )
        _ = assert(r2.isEmpty, "Should not return the excluded vessel")
        r3 <- repo.getVesselsWithVotes(
          bodyId,
          excludedHashes = Seq.empty,
          take = 10,
          allowablePartsOpt = Some(Seq("part1", "part2", "part3", "part4"))
        )
        _ = assert(
          r3 == List((hash, testVessel, 0)),
          "Should return the vessel with allowable parts"
        )
        r4 <- repo.getVesselsWithVotes(
          bodyId,
          excludedHashes = Seq.empty,
          take = 10,
          allowablePartsOpt = Some(Seq("part1", "part2"))
        )
        _ = assert(
          r4.isEmpty,
          "Should not return the vessel when request is missing allowable parts"
        )

        _ <- repo.upsertVote("ip1", hash, bodyId, 1)
        _ <- repo.upsertVote("ip2", hash, bodyId, 1)
        _ <- repo.upsertVote("ip2", hash, bodyId, 1)
        _ <- repo.upsertVote("ip3", hash, bodyId, -1)

        votes <- repo.getVotes(bodyId, testVessel)
        _ = assert(votes == 1, "Vote total should be 1")

        upDownVotes <- repo.getVoteCounts
        _ = assert(
          upDownVotes == (2, 1),
          "Upvote count should be 2 and downvote count should be 1"
        )

        r5 <- repo.getVesselsWithVotes(
          bodyId,
          excludedHashes = Seq.empty,
          take = 10,
          allowablePartsOpt = None
        )
        _ = assert(
          r5 == List((hash, testVessel, 1)),
          "Should return the vessel with correct vote score"
        )

        vesselSlightlyModified = testVessel.copy(
          partSpecs = testVessel.partSpecs.map(spec => spec.copy(position = spec.position.map(p => p.copy(x = p.x + 0.1f)))),
          orbitSpec = testVessel.orbitSpec.map(_.copy(semiMajorAxis = 20000.0))
        )

        _ <- repo.upsertVessel(bodyId, vesselSlightlyModified)
        r6 <- repo.getVesselsWithVotes(
          bodyId,
          excludedHashes = Seq.empty,
          take = 10,
          allowablePartsOpt = None
        )
        _ = assert(
          r6 == List((hash, vesselSlightlyModified, 1)),
          "Should return the updated vessel"
        )

        _     <- repo.upsertVessel(bodyId + 1, testVessel)
        count <- repo.getVehicleCount
        _ = assert(count == 2, "There should be 2 vessels in total")

        r7 <- repo.getVesselsWithVotes(
          bodyId + 1,
          excludedHashes = Seq.empty,
          take = 10,
          allowablePartsOpt = None
        )
        _ = assert(
          r7 == List((hash, testVessel, 0)),
          "Should return the vessel for bodyId + 1"
        )

        vesselWithNewPart = testVessel.copy(
          partSpecs = testVessel.partSpecs :+ PartSpec(
            name = "part4",
            parentIndex = 0,
            position = Some(Vector3(x = 0.0f, y = 0.0f, z = 1.0f)),
            rotation = Some(Quaternion(x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f)),
            attachments = Seq(),
            surfaceAttachment = None
          )
        )
        vesselWithNewPartHash = vesselHash(vesselWithNewPart)

        _ <- repo.upsertVessel(bodyId, vesselWithNewPart)
        r8 <- repo.getVesselsWithVotes(
          bodyId,
          excludedHashes = Seq.empty,
          take = 10,
          allowablePartsOpt = None
        )
        _ = assert(
          r8.sortBy(_._1) == List(
            (vesselWithNewPartHash, vesselWithNewPart, 0),
            (hash, vesselSlightlyModified, 1)
          ).sortBy(_._1),
          "Should return both vessels"
        )
      } yield ()
    }
  }

  val testVessel = VesselSpec(
    orbitSpec = Some(
      OrbitSpec(
        semiMajorAxis = 10000.0,
        eccentricity = 0.1,
        inclination = 45.0,
        argumentOfPeriapsis = 120.0,
        longitudeOfAscendingNode = 80.0,
        meanAnomalyAtEpoch = 30.0,
        epoch = 123456.0
      )
    ),
    partSpecs = Seq(
      PartSpec(
        name = "part1",
        parentIndex = 0,
        position = Some(Vector3(x = 0.0f, y = 0.0f, z = 0.0f)),
        rotation = Some(Quaternion(x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f)),
        attachments = Seq("node_stack_top,0", "node_stack_bottom,1"),
        surfaceAttachment = Some("srfAttach,2"),
        moduleVariantName = Some("variantA")
      ),
      PartSpec(
        name = "part2",
        parentIndex = 0,
        position = Some(Vector3(x = 1.0f, y = 0.0f, z = 0.0f)),
        rotation = Some(Quaternion(x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f)),
        attachments = Seq("node_stack_top,0"),
        surfaceAttachment = None,
        moduleVariantName = None
      ),
      PartSpec(
        name = "part3",
        parentIndex = 1,
        position = Some(Vector3(x = 0.0f, y = 1.0f, z = 0.0f)),
        rotation = Some(Quaternion(x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f)),
        attachments = Seq(),
        surfaceAttachment = None,
        moduleVariantName = Some("")
      )
    )
  )

  def withRepo[A](test: VesselRepository => IO[A]): IO[A] =
    withContainers { container =>
      val deps = for {
        ce <- ExecutionContexts.fixedThreadPool[IO](2)
        xa <- Database.transactorFor(
          container.jdbcUrl,
          container.username,
          container.password
        )
        repo = new VesselRepository(xa)
      } yield repo

      deps.use { repo =>
        test(repo)
      }
    }
}
