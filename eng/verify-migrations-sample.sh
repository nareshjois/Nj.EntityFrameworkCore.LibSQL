#!/usr/bin/env bash
# G9 smoke: database update + run, migration scripts, and dbcontext scaffold.
# Usage:
#   ./eng/verify-migrations-sample.sh
#   LIBSQL_CONNECTION='Data Source=http://127.0.0.1:8080' ./eng/verify-migrations-sample.sh --remote
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

REMOTE=0
for arg in "$@"; do
  case "$arg" in
    --remote) REMOTE=1 ;;
  esac
done

EF_VERSION="10.0.10"
if ! command -v dotnet-ef >/dev/null 2>&1; then
  echo "Installing dotnet-ef ${EF_VERSION}..."
  dotnet tool install --global dotnet-ef --version "$EF_VERSION"
else
  INSTALLED="$(dotnet ef --version 2>/dev/null | tr -d '\r' | tail -1 || true)"
  if [[ "$INSTALLED" != "$EF_VERSION" ]]; then
    echo "Updating dotnet-ef to ${EF_VERSION} (was: ${INSTALLED})..."
    dotnet tool update --global dotnet-ef --version "$EF_VERSION"
  fi
fi

SAMPLE="samples/MigrationsSample/MigrationsSample.csproj"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/nj-g9-XXXXXX")"
SCAFFOLD_DIR="$ROOT/samples/MigrationsSample/_g9_scaffold"
cleanup() {
  rm -rf "$WORK" "$SCAFFOLD_DIR"
}
trap cleanup EXIT

dotnet build "$SAMPLE" -c Release

echo "==> Path A: database update + run (checked-in InitialCreate)"
if [[ "$REMOTE" -eq 1 ]]; then
  if [[ -z "${LIBSQL_CONNECTION:-}" ]]; then
    echo "ERROR: --remote requires LIBSQL_CONNECTION" >&2
    exit 1
  fi
else
  export LIBSQL_CONNECTION="Data Source=${WORK}/migrate.db"
fi

dotnet ef database update --project "$SAMPLE"
dotnet run --project "$SAMPLE" -c Release --no-build

echo "==> Non-idempotent migrations script"
dotnet ef migrations script --project "$SAMPLE" --output "$WORK/migrate.sql" >/dev/null
grep -q 'CREATE TABLE' "$WORK/migrate.sql"

echo "==> Idempotent migrations script (expect failure)"
set +e
dotnet ef migrations script --idempotent --project "$SAMPLE" --output "$WORK/idempotent.sql" \
  >"$WORK/idempotent.out" 2>"$WORK/idempotent.err"
IDEM_EXIT=$?
set -e
if [[ "$IDEM_EXIT" -eq 0 ]]; then
  echo "ERROR: idempotent script should fail" >&2
  cat "$WORK/idempotent.out" "$WORK/idempotent.err" >&2 || true
  exit 1
fi

if [[ "$REMOTE" -eq 1 ]]; then
  echo "G9 MigrationsSample verify succeeded (remote Path A)."
  exit 0
fi

echo "==> Path B: seed schema + dbcontext scaffold"
export LIBSQL_CONNECTION="Data Source=${WORK}/scaffold-source.db"
dotnet run --project "$SAMPLE" -c Release --no-build -- --apply-seed

rm -rf "$SCAFFOLD_DIR"
dotnet ef dbcontext scaffold "$LIBSQL_CONNECTION" Nj.EntityFrameworkCore.LibSql \
  --project "$SAMPLE" \
  --startup-project "$SAMPLE" \
  --output-dir _g9_scaffold \
  --context ScaffoldedContext \
  --no-onconfiguring \
  --force

CONTEXT_FILE="$SCAFFOLD_DIR/ScaffoldedContext.cs"
if [[ ! -f "$CONTEXT_FILE" ]]; then
  echo "ERROR: expected $CONTEXT_FILE" >&2
  find "$ROOT/samples/MigrationsSample" -name 'ScaffoldedContext.cs' >&2 || true
  exit 1
fi

grep -q 'class Blog' "$SCAFFOLD_DIR"/*.cs
grep -q 'class Post' "$SCAFFOLD_DIR"/*.cs
grep -q 'DbSet' "$CONTEXT_FILE"
# Provider wired via design-time services even when --no-onconfiguring.
grep -R --include='*.cs' -q 'UseLibSql\|Nj.EntityFrameworkCore.LibSql' "$SCAFFOLD_DIR" \
  || grep -q 'ScaffoldedContext' "$CONTEXT_FILE"

echo "G9 MigrationsSample verify succeeded (local)."
