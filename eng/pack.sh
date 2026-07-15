#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
mkdir -p artifacts/packages
dotnet pack src/Nj.EntityFrameworkCore.LibSql/Nj.EntityFrameworkCore.LibSql.csproj \
  -c Release \
  -o "$ROOT/artifacts/packages" \
  "$@"
ls -la artifacts/packages
