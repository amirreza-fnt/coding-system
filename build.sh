#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

echo "Building Release..."
dotnet build RequestCodingService.sln -c Release

echo "Publishing to publish/ ..."
dotnet publish src/RequestCodingService.Api/RequestCodingService.Api.csproj \
  -c Release \
  -o publish \
  --self-contained false

echo "Done. Output: $ROOT/publish"
