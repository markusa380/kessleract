package io.github.markusa380.kessleractserver

import cats.effect._
import com.zaxxer.hikari.HikariConfig
import doobie.hikari.HikariTransactor

object Database:

  lazy val dbUrl      = sys.env.getOrElse("DB_URL", "jdbc:postgresql://localhost:5432/kessleract")
  lazy val dbUsername = sys.env.getOrElse("DB_USER", "kessleract")
  lazy val dbPassword = sys.env.getOrElse("DB_PASSWORD", "kessleract")

  val transactor: Resource[IO, HikariTransactor[IO]] =
    for {
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

  val migrate: IO[Unit] = IO {
    val flyway = org.flywaydb.core.Flyway
      .configure()
      .dataSource(dbUrl, dbUsername, dbPassword)
      .load()
    val _ = flyway.migrate()
  }
