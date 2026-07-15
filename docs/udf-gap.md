# UDF / collation gap (Nelknet)

Microsoft EF SQLite registers managed SQL helpers on each connection so LINQ can
emit exact `decimal` math and `Regex.IsMatch` via `CreateFunction` /
`CreateCollation`. Nelknet.LibSQL.Data does **not** expose those ADO.NET APIs
(see [limitations.md](limitations.md)), but **libSQL itself** already provides
native `REGEXP` / `regexp()` (PCRE2). Decimal paths are rewritten to REAL.

## Matrix

| Feature | SQL / name | Translator | LINQ shapes | Status |
|---------|------------|------------|-------------|--------|
| Decimal negate | was `ef_negate` | `LibSqlSqlTranslatingExpressionVisitor` | `-decimal` | **Rewritten** → `CAST` + unary `-` on REAL |
| Decimal arithmetic | was `ef_add` / `ef_multiply` / `ef_divide` | same | `+` `-` `*` `/` on `decimal` | **Rewritten** → REAL ops + `CAST` back |
| Decimal compare | was `ef_compare` | same | `<` `>` `<=` `>=` on `decimal` | **Rewritten** → REAL compares |
| Decimal modulo | was `ef_mod` | same | `%` on `decimal` | **Rewritten** → REAL `%` |
| Decimal aggregates | was `ef_avg` / `ef_sum` / `ef_min` / `ef_max` | `LibSqlQueryableAggregateMethodTranslator` | `Average`/`Sum`/`Min`/`Max` on `decimal` | **Rewritten** → `avg`/`sum`/`min`/`max` on REAL |
| Decimal ordering | was `COLLATE EF_DECIMAL` | `LibSqlQueryableMethodTranslatingExpressionVisitor` | `OrderBy`/`ThenBy` on `decimal` | **Rewritten** → `ORDER BY CAST(… AS REAL)` |
| Regex | `REGEXP` / `regexp()` | `LibSqlRegexMethodTranslator` | `Regex.IsMatch` | **Native libSQL** (PCRE2; not .NET regex) |

## Precision / dialect notes

- **Decimal:** stored as TEXT (invariant string). Rewritten operators cast to
  SQLite `REAL` (IEEE double). Results are **not** exact `System.Decimal`
  semantics.
- **Regex:** translates to libSQL `match REGEXP pattern`. Engine is **PCRE2**,
  not `System.Text.RegularExpressions`. Patterns that rely on .NET-only
  constructs may behave differently; prefer testing patterns against libSQL.
  Full sqlean helpers such as `regexp_like` / `regexp_replace` are **not**
  required for EF’s `Regex.IsMatch` path and may be missing depending on build.

## Historical note

WP-04 fail-fast assumed stock SQLite’s missing `regexp` UDF. Nelknet’s embedded
libSQL already registers `REGEXP` / `regexp()` without ADO.NET `CreateFunction`.

## Waiver

`C-001` in [compatibility.md](compatibility.md) documents the intentional
REAL-precision and PCRE2-vs-.NET dialect differences (not “unsupported”).
