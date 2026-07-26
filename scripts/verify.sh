#!/usr/bin/env bash
set -euo pipefail

# Restores, builds, and tests the entire solution with the repository-local SDK when available.
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_BIN="${DOTNET_BIN:-$ROOT_DIR/.dotnet/dotnet}"

if [[ ! -x "$DOTNET_BIN" ]]; then
  DOTNET_BIN="dotnet"
fi

"$DOTNET_BIN" restore "$ROOT_DIR/CommerceMcpDemo.slnx"
"$DOTNET_BIN" build "$ROOT_DIR/CommerceMcpDemo.slnx" --no-restore
"$DOTNET_BIN" test "$ROOT_DIR/CommerceMcpDemo.slnx" --no-build
