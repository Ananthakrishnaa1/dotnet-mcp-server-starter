#!/usr/bin/env bash
set -euo pipefail

# Builds the combined MCP/API host and keeps all build output off the MCP stdout channel.
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/src/CommerceMcpDemo.McpServer/CommerceMcpDemo.McpServer.csproj"
DLL="$ROOT_DIR/src/CommerceMcpDemo.McpServer/bin/Debug/net10.0/CommerceMcpDemo.McpServer.dll"
DOTNET_BIN="${DOTNET_BIN:-$ROOT_DIR/.dotnet/dotnet}"

if [[ ! -x "$DOTNET_BIN" ]]; then
  DOTNET_BIN="dotnet"
fi

"$DOTNET_BIN" build "$PROJECT" --nologo 1>&2
exec "$DOTNET_BIN" "$DLL"
