package io.github.markusa380.kessleractserver

import cats.effect._
import com.zaxxer.hikari.HikariConfig
import doobie.hikari.HikariTransactor
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
              IO.raiseError(new Exception("Database password not set in DB_PASSWORD or DB_PASSWORD_FILE environment variables")).toResource
      _ <- Resource.eval(migrate(dbPassword))
      hikariConfig <- Resource.pure {
        val config = new HikariConfig()
        config.setDriverClassName("org.postgresql.Driver")
        config.setJdbcUrl(dbUrl)
        config.setUsername(dbUsername)
        config.setPassword(dbPassword)
        config
      }
      transactor <- HikariTransactor.fromHikariConfig[IO](hikariConfig)
    } yield transactor

  def migrate(password: String): IO[Unit] = IO {
    val flyway = Flyway
      .configure()
      .dataSource(dbUrl, dbUsername, password)
      .load()
    val _ = flyway.migrate()
  }
