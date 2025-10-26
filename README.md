## Protobuf
Run `build_proto.bat` to generate C# and Java classes from the .proto files. It will automatically download the required version of `protoc` if not already present.

## Client
### Setup
In KessleractClient.csproj, replace the value of the `KSPBasePath` property with the path to your KSP installation.

From within the `client` directory, run:

- `dotnet build` to build the client application.

## Server
From within the `server` directory, run:

- `sbt compile` to compile the server application.
- `sbt run` to run the server application.

### Docker
To build the Docker image for the server (requires sbt-native-packager):

```
sbt docker:publishLocal
```

This will create a Docker image named `kessleract-server` (or similar) in your local Docker registry.

To run the server container:

```
docker run -p 8080:8080 \
	-e DB_URL=jdbc:postgresql://<host>:5432/kessleract \
	-e DB_USER=kessleract \
	-e DB_PASSWORD=secret \
	kessleract-server
```

#### Configuration options (environment variables):
- `DB_URL`: JDBC URL for the PostgreSQL database (default: `jdbc:postgresql://localhost:5432/kessleract`)
- `DB_USER`: Database username (default: `kessleract`)
- `DB_PASSWORD`: Database password (default: `secret`)

You must have a PostgreSQL database running and accessible to the container.