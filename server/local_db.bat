@echo off
REM Launch a local PostgreSQL instance using Docker

set POSTGRES_CONTAINER_NAME=local_postgres
set POSTGRES_DB=kessleract
set POSTGRES_USER=kessleract
set POSTGRES_PASSWORD=kessleract
set POSTGRES_PORT=5432
set POSTGRES_VOLUME=local_postgres_data

REM Create the Docker volume if it doesn't exist
docker volume inspect %POSTGRES_VOLUME% >nul 2>nul
if errorlevel 1 docker volume create %POSTGRES_VOLUME%

docker run --rm --name %POSTGRES_CONTAINER_NAME% ^
	-e POSTGRES_DB=%POSTGRES_DB% ^
	-e POSTGRES_USER=%POSTGRES_USER% ^
	-e POSTGRES_PASSWORD=%POSTGRES_PASSWORD% ^
	-p %POSTGRES_PORT%:5432 ^
	-v %POSTGRES_VOLUME%:/var/lib/postgresql ^
	-d postgres

echo PostgreSQL is starting on port %POSTGRES_PORT% with persistent data in volume %POSTGRES_VOLUME%.