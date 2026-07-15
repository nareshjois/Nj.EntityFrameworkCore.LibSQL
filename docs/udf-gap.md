# UDF / collation gap (Nelknet)

Microsoft EF SQLite registers managed SQL helpers on each connection so LINQ can
emit exact `decimal` math and `Regex.IsMatch`. Nelknet.LibSQL.Data does **not**
expose `sqlite3_create_function` / aggregates / collation (see
[limitations.md](limitations.md)).

Until Nelknet adds those APIs **or** this provider rewrites translations to
native SQL (WP-05), the provider **fails at query translation** with
`NotSupportedException` instead of generating SQL that would fail at execute.

## Matrix

| Feature | SQL / name | Translator | LINQ shapes | Status |
|---------|------------|------------|-------------|--------|
| Decimal negate | `ef_negate` | `LibSqlSqlTranslatingExpressionVisitor` | `-decimal` | **Fail translation** |
| Decimal arithmetic | `ef_add` / `ef_multiply` / `ef_divide` | same | `+` `-` `*` `/` on `decimal` | **Fail translation** |
| Decimal compare | `ef_compare` | same | `<` `>` `<=` `>=` on `decimal` | **Fail translation** |
| Decimal modulo | `ef_mod` | same | `%` on `decimal` | **Fail translation** |
| Decimal aggregates | `ef_avg` / `ef_sum` / `ef_min` / `ef_max` | `LibSqlQueryableAggregateMethodTranslator` | `Average`/`Sum`/`Min`/`Max` on `decimal` | **Fail translation** |
| Decimal ordering | `COLLATE EF_DECIMAL` | `LibSqlQueryableMethodTranslatingExpressionVisitor` | `OrderBy`/`ThenBy` on `decimal` | **Fail translation** |
| Regex | `regexp` | `LibSqlRegexMethodTranslator` | `Regex.IsMatch` | **Fail translation** |

## Resolution paths

1. **Upstream Nelknet** — restore registration in `LibSqlRelationalConnection.InitializeDbConnection` (preferred for Microsoft parity).
2. **Rewrite translations** (WP-05) — map to `REAL`/`CAST` / `GLOB`/`LIKE` with documented precision differences; update this table to `rewritten`.
3. Do **not** use `load_extension` — not exposed by Nelknet.

## Waiver

Permanent skips that depend on these helpers must cite this doc in
[compatibility.md](compatibility.md).
