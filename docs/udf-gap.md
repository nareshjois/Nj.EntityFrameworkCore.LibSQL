# UDF / collation gap (Nelknet)

Microsoft EF SQLite registers managed SQL helpers on each connection so LINQ can
emit exact `decimal` math and `Regex.IsMatch`. Nelknet.LibSQL.Data does **not**
expose `sqlite3_create_function` / aggregates / collation (see
[limitations.md](limitations.md)).

## Matrix

| Feature | SQL / name | Translator | LINQ shapes | Status |
|---------|------------|------------|-------------|--------|
| Decimal negate | was `ef_negate` | `LibSqlSqlTranslatingExpressionVisitor` | `-decimal` | **Rewritten** → `CAST` + unary `-` on REAL |
| Decimal arithmetic | was `ef_add` / `ef_multiply` / `ef_divide` | same | `+` `-` `*` `/` on `decimal` | **Rewritten** → REAL ops + `CAST` back |
| Decimal compare | was `ef_compare` | same | `<` `>` `<=` `>=` on `decimal` | **Rewritten** → REAL compares |
| Decimal modulo | was `ef_mod` | same | `%` on `decimal` | **Rewritten** → REAL `%` |
| Decimal aggregates | was `ef_avg` / `ef_sum` / `ef_min` / `ef_max` | `LibSqlQueryableAggregateMethodTranslator` | `Average`/`Sum`/`Min`/`Max` on `decimal` | **Rewritten** → `avg`/`sum`/`min`/`max` on REAL |
| Decimal ordering | was `COLLATE EF_DECIMAL` | `LibSqlQueryableMethodTranslatingExpressionVisitor` | `OrderBy`/`ThenBy` on `decimal` | **Rewritten** → `ORDER BY CAST(… AS REAL)` |
| Regex | `regexp` | `LibSqlRegexMethodTranslator` | `Regex.IsMatch` | **Fail translation** |

## Precision note

Decimals are stored as TEXT (invariant string). Rewritten operators cast to
SQLite `REAL` (IEEE double). Results are **not** exact `System.Decimal`
semantics — values near the limits of double precision may round differently
from Microsoft EF SQLite’s `ef_*` helpers. Prefer client evaluation when exact
decimal arithmetic is required.

## Resolution paths

1. **Upstream Nelknet** — restore registration in `LibSqlRelationalConnection.InitializeDbConnection` (preferred for Microsoft parity / `regexp`).
2. **Rewrite translations** — decimal paths use option 2 (this doc); `regexp` still fails.
3. Do **not** use `load_extension` — not exposed by Nelknet.

## Waiver

Permanent skips that depend on remaining fail-fast helpers (`regexp`) must cite
this doc in [compatibility.md](compatibility.md) (`C-001`).
