val Http4sVersion = "0.23.30"
val MunitVersion = "1.1.0"
val LogbackVersion = "1.5.16"
val MunitCatsEffectVersion = "2.0.0"
val CirceVersion = "0.14.12"
val ProtobufVersion = "4.33.0"
val FlywayVersion = "11.15.0"
val DoobieVersion = "1.0.0-RC10"

lazy val root = (project in file("."))
  .enablePlugins(JavaAppPackaging, DockerPlugin)
  .settings(
    organization := "io.github.markusa380",
    name := "kessleract-server",
    version := "0.0.1-SNAPSHOT",
    scalaVersion := "3.3.5",
    semanticdbEnabled := true,
    semanticdbVersion := scalafixSemanticdb.revision,
    dockerExposedPorts := Seq(8080),
    libraryDependencies ++= Seq(
      "org.http4s" %% "http4s-ember-server" % Http4sVersion,
      "org.http4s" %% "http4s-ember-client" % Http4sVersion,
      "org.http4s" %% "http4s-circe" % Http4sVersion,
      "org.http4s" %% "http4s-dsl" % Http4sVersion,
      "org.scalameta" %% "munit" % MunitVersion % Test,
      "org.typelevel" %% "munit-cats-effect" % MunitCatsEffectVersion % Test,
      "ch.qos.logback" % "logback-classic" % LogbackVersion,
      "io.circe" %% "circe-core" % CirceVersion,
      "io.circe" %% "circe-parser" % CirceVersion,
      "io.circe" %% "circe-generic" % CirceVersion,
      "com.google.protobuf" % "protobuf-java" % ProtobufVersion,
      "com.google.protobuf" % "protobuf-java-util" % ProtobufVersion,
      "org.tpolecat" %% "doobie-core" % DoobieVersion,
      "org.tpolecat" %% "doobie-postgres" % DoobieVersion,
      "org.tpolecat" %% "doobie-hikari" % DoobieVersion,
      "org.flywaydb" % "flyway-core" % FlywayVersion,
      "org.flywaydb" % "flyway-database-postgresql" % FlywayVersion,
    ),
    assembly / assemblyMergeStrategy := {
      case "module-info.class" => MergeStrategy.discard
      case x => (assembly / assemblyMergeStrategy).value.apply(x)
    }
  )
