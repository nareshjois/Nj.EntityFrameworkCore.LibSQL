#!/usr/bin/env bash
# Compare the renamed provider tree to the recorded EFCore.Sqlite.Core baseline.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TAG="${EFCORE_SQLITE_BASELINE_TAG:-v10.0.10}"
COMMIT="${EFCORE_SQLITE_BASELINE_COMMIT:-db55508a7fbc1535bdb65b85159a8d0d36d6942a}"
CACHE="${EFCORE_SQLITE_COMPARE_CACHE:-/tmp/efcore-sqlite-baseline-${TAG}}"
OUT_DIR="${ROOT}/artifacts/upstream-diff"
LOCAL_SRC="${ROOT}/src/Nj.EntityFrameworkCore.LibSql"
WORK="${OUT_DIR}/work"

mkdir -p "${OUT_DIR}" "${WORK}"

if [[ ! -d "${CACHE}/.git" ]]; then
  rm -rf "${CACHE}"
  mkdir -p "${CACHE}"
  git -C "${CACHE}" init -q
  git -C "${CACHE}" remote add origin https://github.com/dotnet/efcore.git
  git -C "${CACHE}" config core.sparseCheckout true
  {
    echo "src/EFCore.Sqlite.Core/*"
    echo "src/Shared/*"
  } > "${CACHE}/.git/info/sparse-checkout"
  git -C "${CACHE}" fetch --depth 1 origin "refs/tags/${TAG}:refs/tags/${TAG}"
  git -C "${CACHE}" checkout -q "${COMMIT}"
fi

UP_CORE="${CACHE}/src/EFCore.Sqlite.Core"
UP_SHARED="${CACHE}/src/Shared"

rm -rf "${WORK}/upstream" "${WORK}/local-as-sqlite"
mkdir -p "${WORK}/upstream" "${WORK}/local-as-sqlite"

rsync -a --exclude 'EFCore.Sqlite.Core.csproj' --exclude 'PACKAGE.md' --exclude 'bin' --exclude 'obj' \
  "${UP_CORE}/" "${WORK}/upstream/"
mkdir -p "${WORK}/upstream/Shared"
rsync -a "${UP_SHARED}/" "${WORK}/upstream/Shared/"

rsync -a \
  --exclude 'bin' --exclude 'obj' \
  --exclude 'Nj.EntityFrameworkCore.LibSql.csproj' \
  --exclude 'LibSqlProviderInfo.cs' \
  "${LOCAL_SRC}/" "${WORK}/local-as-sqlite/"

export COMPARE_UPSTREAM="${WORK}/upstream"
export COMPARE_LOCAL="${WORK}/local-as-sqlite"

python3 - <<'PY'
import os
from pathlib import Path

text_exts = {".cs", ".resx", ".tt", ".md"}
protect = [
    ("Microsoft.Data.Sqlite", "[[[MDS]]]"),
    ("SqliteConnectionStringBuilder", "[[[SCSB]]]"),
    ("SqliteConnection", "[[[SC]]]"),
    ("SqliteException", "[[[SE]]]"),
    ("SqliteOpenMode", "[[[SOM]]]"),
    ("SqliteErrorCode", "[[[SEC]]]"),
]
replacements = [
    ("Nj.EntityFrameworkCore.LibSql", "Microsoft.EntityFrameworkCore.Sqlite"),
    ("AddEntityFrameworkLibSql", "AddEntityFrameworkSqlite"),
    ("LibSqlStrings", "SqliteStrings"),
    ("LibSqlResources", "SqliteResources"),
    ("ILibSql", "ISqlite"),
    ("LibSql", "Sqlite"),
]


def normalize_bytes(path: Path) -> None:
    data = path.read_bytes()
    if data.startswith(b"\xef\xbb\xbf"):
        data = data[3:]
    if data and not data.endswith(b"\n"):
        data += b"\n"
    path.write_bytes(data)


def normalize_tree(root: Path) -> None:
    for path in sorted(root.rglob("*")):
        if path.is_file() and path.suffix in text_exts:
            normalize_bytes(path)


def reverse_rename_local(root: Path) -> None:
    for path in sorted(root.rglob("*")):
        if not path.is_file() or path.suffix not in text_exts:
            continue
        text = path.read_text(encoding="utf-8")
        for a, b in protect:
            text = text.replace(a, b)
        for a, b in replacements:
            text = text.replace(a, b)
        for a, b in protect:
            text = text.replace(b, a)
        path.write_text(text, encoding="utf-8", newline="\n")

    for path in sorted(root.rglob("*"), key=lambda p: len(p.parts), reverse=True):
        if not path.is_file():
            continue
        name = path.name
        new_name = name.replace("ILibSql", "ISqlite").replace("LibSql", "Sqlite")
        if new_name != name:
            path.rename(path.with_name(new_name))


upstream = Path(os.environ["COMPARE_UPSTREAM"])
local = Path(os.environ["COMPARE_LOCAL"])
normalize_tree(upstream)
normalize_tree(local)
reverse_rename_local(local)
PY

REPORT="${OUT_DIR}/report.txt"
{
  echo "Baseline tag=${TAG} commit=${COMMIT}"
  echo "Generated=$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo
  diff -ruN "${WORK}/upstream" "${WORK}/local-as-sqlite" || true
} > "${REPORT}"

DIFF_COUNT="$(grep -c '^diff ' "${REPORT}" || true)"
echo "Wrote ${REPORT} (${DIFF_COUNT} file hunks)."
exit 0
