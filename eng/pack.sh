#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
mkdir -p artifacts/packages
for proj in \
  src/Nj.LibSql.Bindings/Nj.LibSql.Bindings.csproj \
  src/Nj.LibSql.Data/Nj.LibSql.Data.csproj \
  src/Nj.EntityFrameworkCore.LibSql/Nj.EntityFrameworkCore.LibSql.csproj
do
  dotnet pack "$proj" -c Release -o "$ROOT/artifacts/packages" "$@"
done
ls -la artifacts/packages
