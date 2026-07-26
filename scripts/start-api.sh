#!/usr/bin/env bash
set -euo pipefail

# Starts the HTTP-only API host; it has its own process-local deterministic sample data.
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_BIN="${DOTNET_BIN:-$ROOT_DIR/.dotnet/dotnet}"

if [[ ! -x "$DOTNET_BIN" ]]; then
  DOTNET_BIN="dotnet"
fi

exec "$DOTNET_BIN" run --project "$ROOT_DIR/src/CommerceMcpDemo.Api/CommerceMcpDemo.Api.csproj" --no-launch-profile
