#!/usr/bin/env bash
# Local gate matching PR checks for the Nj.LibSql driver workstream.
# Run before push: ./eng/verify-driver-ci-local.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

OS="$(uname -s)"
echo "==> restore solution"
dotnet restore Nj.EntityFrameworkCore.LibSql.slnx

if [[ "${OS}" == "Linux" || "${OS}" == "Darwin" ]]; then
  echo "==> format verify (same excludes as ci.yml / libsql-driver Linux)"
  dotnet format Nj.EntityFrameworkCore.LibSql.slnx --verify-no-changes --no-restore \
    --exclude ./src/Nj.EntityFrameworkCore.LibSql/**
fi

echo "==> build solution (Release)"
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release --no-restore

echo "==> libsql-driver local tests"
dotnet test test/Nj.LibSql.DriverContractTests/Nj.LibSql.DriverContractTests.csproj \
  -c Release --no-build \
  --filter "FullyQualifiedName!~Remote&FullyQualifiedName!~Turso"

echo "==> libsql-driver remote-sqld (Testcontainers; requires Docker)"
export LIBSQL_REQUIRE_REMOTE="${LIBSQL_REQUIRE_REMOTE:-1}"
# Prefer Testcontainers over an external Turso URL for this job.
env -u LIBSQL_TEST_URL -u LIBSQL_TEST_AUTH_TOKEN \
  LIBSQL_REQUIRE_REMOTE="${LIBSQL_REQUIRE_REMOTE}" \
  dotnet test test/Nj.LibSql.DriverContractTests/Nj.LibSql.DriverContractTests.csproj \
  -c Release --no-build \
  --filter "FullyQualifiedName~Remote&FullyQualifiedName!~Turso"

if [[ -n "${LIBSQL_TEST_URL:-}" && -n "${LIBSQL_TEST_AUTH_TOKEN:-}" ]]; then
  echo "==> libsql-driver turso"
  export LIBSQL_REQUIRE_TURSO=1
  export LIBSQL_DISABLE_TESTCONTAINERS=1
  dotnet test test/Nj.LibSql.DriverContractTests/Nj.LibSql.DriverContractTests.csproj \
    -c Release --no-build \
    --filter "FullyQualifiedName~Turso"
else
  echo "==> skip turso (set LIBSQL_TEST_URL + LIBSQL_TEST_AUTH_TOKEN to run)"
fi

echo "OK: local driver CI gate passed"
