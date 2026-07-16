#!/usr/bin/env bash
# Run ComplianceTests and emit pass/fail/waived summary artifacts.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="${ROOT}/artifacts/compliance"
mkdir -p "${OUT_DIR}"

CONFIG="${CONFIG:-Release}"
FILTER="${FILTER:-}"
MODE="${MODE:-local}"
TRX="${OUT_DIR}/compliance-${MODE}.trx"
JSON="${OUT_DIR}/compliance-${MODE}.json"
MD="${OUT_DIR}/compliance-${MODE}.md"

ARGS=(
  test "${ROOT}/test/Nj.EntityFrameworkCore.LibSql.ComplianceTests/Nj.EntityFrameworkCore.LibSql.ComplianceTests.csproj"
  -c "${CONFIG}"
  --logger "trx;LogFileName=$(basename "${TRX}")"
  --results-directory "${OUT_DIR}"
  --logger "console;verbosity=minimal"
)

if [[ -n "${FILTER}" ]]; then
  ARGS+=(--filter "${FILTER}")
fi

if [[ "${MODE}" == "remote" ]]; then
  if [[ -z "${LIBSQL_TEST_URL:-}" ]]; then
    echo "LIBSQL_TEST_URL is required for remote compliance runs" >&2
    exit 1
  fi
  export LIBSQL_TEST_URL
  ARGS+=(--filter "FullyQualifiedName~Remote")
elif [[ -z "${FILTER}" ]]; then
  # Local gate excludes remote-only suites (C-016).
  ARGS+=(--filter "FullyQualifiedName!~BuiltInDataTypesRemoteLibSqlTest")
fi

echo "Running compliance tests (mode=${MODE}, config=${CONFIG})..."
set +e
dotnet "${ARGS[@]}"
TEST_EXIT=$?
set -e

python3 - "${TRX}" "${JSON}" "${MD}" "${MODE}" <<'PY'
import json
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from datetime import datetime, timezone

trx_path, json_path, md_path, mode = sys.argv[1:5]
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
root = ET.parse(trx_path).getroot()
results = root.find("t:Results", ns)
counter = Counter()
by_suite: dict[str, Counter] = {}

for unit in results.findall("t:UnitTestResult", ns):
    outcome = unit.attrib.get("outcome", "Unknown")
    counter[outcome] += 1
    name = unit.attrib.get("testName", "")
    suite = name.rsplit(".", 1)[0] if "." in name else name
    by_suite.setdefault(suite, Counter())[outcome] += 1

summary = {
    "generatedAt": datetime.now(timezone.utc).isoformat(),
    "mode": mode,
    "totals": dict(counter),
    "suites": {k: dict(v) for k, v in sorted(by_suite.items())},
}
with open(json_path, "w", encoding="utf-8") as f:
    json.dump(summary, f, indent=2)

passed = counter.get("Passed", 0)
failed = counter.get("Failed", 0)
skipped = counter.get("NotExecuted", 0) + counter.get("Skipped", 0)
total = sum(counter.values())

lines = [
    f"# Compliance report ({mode})",
    "",
    f"- Generated: {summary['generatedAt']}",
    f"- Total: {total}",
    f"- Passed: {passed}",
    f"- Failed: {failed}",
    f"- Skipped/waived: {skipped}",
    "",
    "## By suite",
    "",
    "| Suite | Passed | Failed | Skipped |",
    "|-------|--------|--------|---------|",
]
for suite, counts in sorted(by_suite.items()):
    lines.append(
        f"| `{suite}` | {counts.get('Passed', 0)} | {counts.get('Failed', 0)} | "
        f"{counts.get('NotExecuted', 0) + counts.get('Skipped', 0)} |"
    )
lines.append("")
with open(md_path, "w", encoding="utf-8") as f:
    f.write("\n".join(lines))

print(f"Wrote {json_path} and {md_path}")
PY

echo "Compliance report artifacts in ${OUT_DIR}"
exit "${TEST_EXIT}"
