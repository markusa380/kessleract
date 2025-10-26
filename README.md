# Commands

## Protobuf
Run `build_proto.bat` to generate C# and Java classes from the .proto files. It will automatically download the required version of `protoc` if not already present.

## Client
From within the `client` directory, run:

- `dotnet build` to build the client application.

## Server
From within the `server` directory, run:

- `sbt compile` to compile the server application.
- `sbt run` to run the server application.