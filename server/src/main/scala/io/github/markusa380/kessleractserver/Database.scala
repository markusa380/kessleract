package io.github.markusa380.kessleractserver

import cats.effect._
import cats.syntax.all._
import com.zaxxer.hikari.HikariConfig
import doobie.hikari.HikariTransactor
import doobie.util.log.ExecFailure
import doobie.util.log.LogEvent
import doobie.util.log.LogHandler
import doobie.util.log.ProcessingFailure
import doobie.util.log.Success
import org.flywaydb.core.Flyway

object Database:

  lazy val dbUrl          = sys.env.getOrElse("DB_URL", "jdbc:postgresql://localhost:5432/kessleract")
  lazy val dbUsername     = sys.env.getOrElse("DB_USER", "kessleract")
  lazy val dbPasswordRaw  = sys.env.get("DB_PASSWORD")
  lazy val dbPasswordFile = sys.env.get("DB_PASSWORD_FILE")

  val transactor: Resource[IO, HikariTransactor[IO]] =
    for {
      dbPassword <- dbPasswordRaw match
        case Some(pw) => Resource.pure(pw)
        case None =>
          dbPasswordFile match
            case Some(path) =>
              IO.blocking(scala.io.Source.fromFile(path).getLines().mkString.trim).toResource
            case None =>
              Resource.pure("kessleract")
      _ <- Resource.eval(migrate(dbPassword))
      hikariConfig <- Resource.pure {
        val config = new HikariConfig()
        config.setDriverClassName("org.postgresql.Driver")
        config.setJdbcUrl(dbUrl)
        config.setUsername(dbUsername)
        config.setPassword(dbPassword)
        config
      }
      transactor <- HikariTransactor.fromHikariConfig[IO](hikariConfig, logHandler.some)
    } yield transactor

  val logHandler: LogHandler[IO] = new LogHandler[IO] {
    def run(logEvent: LogEvent): IO[Unit] = logEvent match {
      case s: Success           => log.info(s"[SQL] Success; Exec: ${s.exec}, Processing: ${s.processing}, Params: ${s.params}:\n${s.sql}")
      case e: ExecFailure       => log.error(e.failure)(s"[SQL] Exec Failure: ${e.failure.getMessage}; Exec: ${e.exec}, Params: ${e.params}:\n${e.sql}")
      case e: ProcessingFailure => log.error(e.failure)(s"[SQL] Processing Failure: ${e.failure.getMessage}; Exec: ${e.exec}, Processing: ${e.processing}, Params: ${e.params}:\n${e.sql}")
    }
  }

  def migrate(password: String): IO[Unit] = IO {
    val flyway = Flyway
      .configure()
      .dataSource(dbUrl, dbUsername, password)
      .load()
    val _ = flyway.migrate()
  }
