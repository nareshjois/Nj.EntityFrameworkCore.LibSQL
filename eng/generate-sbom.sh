#!/usr/bin/env bash
# Generate SBOM artifacts under artifacts/sbom (CycloneDX when available; always emit deps JSON).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
mkdir -p artifacts/sbom

export DOTNET_ROOT="${DOTNET_ROOT:-$(dirname "$(command -v dotnet)")}"

EF_PROJ="$ROOT/src/Nj.EntityFrameworkCore.LibSql/Nj.EntityFrameworkCore.LibSql.csproj"
DATA_PROJ="$ROOT/src/Nj.LibSql.Data/Nj.LibSql.Data.csproj"

echo "==> Package dependency list (JSON)"
dotnet list "$EF_PROJ" package --include-transitive --format json \
  > artifacts/sbom/nj-entityframeworkcore-libsql.deps.json
dotnet list "$DATA_PROJ" package --include-transitive --format json \
  > artifacts/sbom/nj-libsql-data.deps.json

echo "==> CycloneDX (best-effort)"
TOOL_DIR="$ROOT/artifacts/tools"
mkdir -p "$TOOL_DIR"
if dotnet tool install CycloneDX --version 5.4.0 --tool-path "$TOOL_DIR" >/dev/null 2>&1 \
  || dotnet tool update CycloneDX --version 5.4.0 --tool-path "$TOOL_DIR" >/dev/null 2>&1; then
  if DOTNET_ROOT="$DOTNET_ROOT" "$TOOL_DIR/dotnet-CycloneDX" \
      "$EF_PROJ" \
      -o "$ROOT/artifacts/sbom" \
      -f json \
      -fn nj-entityframeworkcore-libsql.cdx.json \
      --exclude-dev 2>/dev/null; then
    echo "CycloneDX SBOM written."
  else
    echo "CycloneDX CLI unavailable in this environment; deps JSON is the SBOM artifact."
  fi
else
  echo "CycloneDX tool install skipped; deps JSON is the SBOM artifact."
fi

ls -la artifacts/sbom
