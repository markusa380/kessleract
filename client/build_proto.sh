mkdir -p ./proto
protoc --csharp_out="./proto" --proto_path="../proto" messages.proto service.proto