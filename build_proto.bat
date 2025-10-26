@echo off
REM Script to generate C# classes from messages.proto using protoc
REM Ensure protoc.exe is available in your PATH or specify its location below

set PROTOC_DOWNLOAD_URL=https://github.com/protocolbuffers/protobuf/releases/download/v3.10.1/protoc-3.10.1-win64.zip
set PROTO_FILES=proto\messages.proto proto\service.proto
set OUT_DIR_CLIENT=client\proto
set OUT_DIR_SERVER=server\src\main\scala\proto

set PROTOC_DIR=tools\protoc
set PROTOC_EXE=%PROTOC_DIR%\bin\protoc.exe

REM Download and extract protoc if not already present
if not exist %PROTOC_EXE% (
    echo Downloading protoc...
    powershell -Command "Invoke-WebRequest -Uri '%PROTOC_DOWNLOAD_URL%' -OutFile 'protoc.zip'"
    powershell -Command "Expand-Archive -Path 'protoc.zip' -DestinationPath '%PROTOC_DIR%'"
    del protoc.zip
)

REM Create output directory if it doesn't exist
if not exist %OUT_DIR_CLIENT% mkdir %OUT_DIR_CLIENT%
if not exist %OUT_DIR_SERVER% mkdir %OUT_DIR_SERVER%

REM Generate C# classes
%PROTOC_EXE% --csharp_out=%OUT_DIR_CLIENT% --java_out=%OUT_DIR_SERVER% --proto_path=proto %PROTO_FILES%

if %ERRORLEVEL% neq 0 (
    echo Generation failed.
    exit /b %ERRORLEVEL%
) else (
    echo Generation succeeded.
)
