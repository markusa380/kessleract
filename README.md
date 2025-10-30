## Protobuf

## Setup

If you use Nix, can use `nix develop` to install all requirements.

Otherwise, you need to install the following:
- docker
- sbt 1.10.7+
- OpenJDK 21
- protobuf v30
- .NET SDK 8

## Client

In KessleractClient.csproj, replace the value of the `KSPBasePath` property with the path to your KSP installation.

From within the `client` directory, run:

- `dotnet build` to build the plugin and copy all files for the plugin into your KSP GameData

## Server
From within the `server` directory, run:

- `sbt compile` to compile the server application.
- `sbt startDevDb` to start the development database on localhost
- `sbt run` to run the server application (stop with Ctrl+C)
- `sbt stopDevDb` to stop the development database
- `./deploy.sh` to deploy the stack to a Docker swarm
- `sbt updateServer` to update the image of the server on Docker swarm