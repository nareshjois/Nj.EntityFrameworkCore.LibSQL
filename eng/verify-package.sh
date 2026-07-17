#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

mkdir -p artifacts/packages artifacts/test-results

echo "==> Restore + build"
dotnet restore Nj.EntityFrameworkCore.LibSql.slnx
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release --no-restore

echo "==> Pack (Bindings → Data → EF)"
dotnet pack src/Nj.LibSql.Bindings/Nj.LibSql.Bindings.csproj \
  -c Release -o "$ROOT/artifacts/packages"
dotnet pack src/Nj.LibSql.Data/Nj.LibSql.Data.csproj \
  -c Release -o "$ROOT/artifacts/packages"
dotnet pack src/Nj.EntityFrameworkCore.LibSql/Nj.EntityFrameworkCore.LibSql.csproj \
  -c Release -o "$ROOT/artifacts/packages"

PKG=$(ls "$ROOT/artifacts/packages"/Nj.EntityFrameworkCore.LibSql.*.nupkg | head -1)
echo "Packed: $PKG"

# Drop global-cache copies of our preview packages so restore cannot reuse a
# stale nuspec (e.g. Nelknet-era dependency graph under the same version).
NUGET_PKGS="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
for id in nj.entityframeworkcore.libsql nj.libsql.data nj.libsql.bindings; do
  rm -rf "$NUGET_PKGS/$id"
done

echo "==> Clean package restore + PackageTests against nupkg"
dotnet restore test/Nj.EntityFrameworkCore.LibSql.PackageTests/Nj.EntityFrameworkCore.LibSql.PackageTests.csproj \
  -p:VerifyPackedPackage=true \
  --force \
  --force-evaluate \
  --source "$ROOT/artifacts/packages" \
  --source "https://api.nuget.org/v3/index.json"

dotnet test test/Nj.EntityFrameworkCore.LibSql.PackageTests/Nj.EntityFrameworkCore.LibSql.PackageTests.csproj \
  -c Release \
  --no-restore \
  -p:VerifyPackedPackage=true \
  --logger "trx;LogFileName=package-tests.trx" \
  --results-directory "$ROOT/artifacts/test-results"

echo "==> Run LocalSample"
dotnet run --project samples/LocalSample/LocalSample.csproj -c Release --no-build

echo "Package verification succeeded."
