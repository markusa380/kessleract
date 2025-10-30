val Http4sVersion          = "0.23.30"
val MunitVersion           = "1.1.0"
val LogbackVersion         = "1.5.16"
val MunitCatsEffectVersion = "2.0.0"
val CirceVersion           = "0.14.12"
val ProtobufVersion        = "4.33.0"
val FlywayVersion          = "11.15.0"
val DoobieVersion          = "1.0.0-RC10"
val ScalaPbCirceVersion    = "0.16.0"
val Log4CatsVersion        = "2.7.1"

lazy val startDevDb   = taskKey[Unit]("Starts a local development database using Docker")
lazy val stopDevDb    = taskKey[Unit]("Stops local development database")
lazy val updateServer = taskKey[Unit]("Deploys the server to the production environment")

Global / onChangedBuildSource := ReloadOnSourceChanges

lazy val root = (project in file("."))
  .enablePlugins(JavaAppPackaging, DockerPlugin)
  .settings(
    organization       := "io.github.markusa380",
    name               := "kessleract-server",
    version            := "0.0.1-SNAPSHOT",
    scalaVersion       := "3.3.5",
    semanticdbEnabled  := true,
    semanticdbVersion  := scalafixSemanticdb.revision,
    dockerExposedPorts := Seq(8080),
    dockerBaseImage    := "eclipse-temurin:21",
    libraryDependencies ++= Seq(
      "org.http4s"             %% "http4s-ember-server"        % Http4sVersion,
      "org.http4s"             %% "http4s-ember-client"        % Http4sVersion,
      "org.http4s"             %% "http4s-circe"               % Http4sVersion,
      "org.http4s"             %% "http4s-dsl"                 % Http4sVersion,
      "org.scalameta"          %% "munit"                      % MunitVersion           % Test,
      "org.typelevel"          %% "munit-cats-effect"          % MunitCatsEffectVersion % Test,
      "ch.qos.logback"          % "logback-classic"            % LogbackVersion,
      "io.circe"               %% "circe-core"                 % CirceVersion,
      "io.circe"               %% "circe-parser"               % CirceVersion,
      "io.circe"               %% "circe-generic"              % CirceVersion,
      "com.google.protobuf"     % "protobuf-java"              % ProtobufVersion,
      "com.google.protobuf"     % "protobuf-java-util"         % ProtobufVersion,
      "org.tpolecat"           %% "doobie-core"                % DoobieVersion,
      "org.tpolecat"           %% "doobie-postgres"            % DoobieVersion,
      "org.tpolecat"           %% "doobie-hikari"              % DoobieVersion,
      "org.flywaydb"            % "flyway-core"                % FlywayVersion,
      "org.flywaydb"            % "flyway-database-postgresql" % FlywayVersion,
      "io.github.scalapb-json" %% "scalapb-circe"              % ScalaPbCirceVersion,
      "io.github.scalapb-json" %% "scalapb-circe-macros"       % ScalaPbCirceVersion,
      "org.typelevel"          %% "log4cats-core"              % Log4CatsVersion,
      "org.typelevel"          %% "log4cats-slf4j"             % Log4CatsVersion,
      "com.lihaoyi"            %% "scalatags"                  % "0.12.0"
    ),
    assembly / assemblyMergeStrategy := {
      case "module-info.class" => MergeStrategy.discard
      case x                   => (assembly / assemblyMergeStrategy).value.apply(x)
    },
    Compile / PB.targets := Seq(
      scalapb.gen() -> (Compile / sourceManaged).value / "scalapb"
    ),
    Compile / PB.protoSources  := Seq(baseDirectory.value / ".." / "proto"),
    Compile / PB.protocVersion := "-v3.33.0",
    // Hack: https://github.com/scalapb/ScalaPB/issues/1816#issuecomment-2909209798
    Compile / PB.generate ~= { files =>
      files.filter(_.isFile).foreach { file =>
        val fileContent = IO.read(file)
        val updatedContent = fileContent.replace(
          "  _unknownFields__.parseField(tag, _input__)",
          "val _ = _unknownFields__.parseField(tag, _input__)"
        )
        IO.write(file, updatedContent)
      }
      files
    },
    startDevDb := {
      import sys.process._
      val currentContext = "docker context show".!!.trim
      if (currentContext != "default") {
        throw new Exception(s"Current Docker context is '$currentContext'. Please switch to the 'default' context to start the database.")
      }
      val dockerCmd = "docker run --name kessleract-db --rm -d" +
        " -v kessleract_db:/var/lib/postgresql " +
        "-p 5432:5432" +
        " -e POSTGRES_USER=kessleract" +
        " -e POSTGRES_PASSWORD=kessleract" +
        " -e POSTGRES_DB=kessleract" +
        " postgres:18"
      val containerId = dockerCmd.!!.trim
      println(s"Started development database container (ID: $containerId)")
    },
    stopDevDb := {
      import sys.process._
      val currentContext = "docker context show".!!.trim
      if (currentContext != "default") {
        throw new Exception(s"Current Docker context is '$currentContext'. Please switch to the 'default' context to start the database.")
      }
      "docker stop kessleract-db".!!
      println("Stopped development database container")
    },
    updateServer := {
      import sys.process._
      val _         = (Docker / publishLocal).value
      val serviceId = "docker stack services kessleract --filter name=kessleract_server --quiet".!!.trim
      s"docker service update --force $serviceId".!!
    }
  )
