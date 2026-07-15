# WP-06 handoff

**Status:** first slice on `wp-06-query-translation` (FunctionalTests LINQ matrix).

## Done

- Local + remote LINQ matrix covering filter/project, order/page, join, include,
  group/aggregate (int), union, strings, math, DateTime members, Guid/bytes,
  JSON owned + primitive collections, `FromSqlInterpolated`, sync/async parity,
  and `TagWith` (thin SQL capture via `LogTo`).
- Soft-fork Nelknet `@b0a9c51`: normalize unprefixed parameter names so EF
  `FromSqlInterpolated` (`p0` vs `@p0`) binds (with prior RETURNING + HTTP baton
  patches).
- UDF fail-fast retained (`C-001`); added decimal `Average` → `ef_avg` coverage.

## Deferred (not this slice)

- Full G6 EF relational / SQLite specification suites (WP-10 fixtures).
- TPH inheritance, compiled queries/models, Glob/Hex/Substring goldens,
  interceptor suites.
- UDF / `ef_*` / `regexp` / `EF_DECIMAL` rewrite (`C-001`).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Query
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~UdfGap
```

## Next

- Deeper WP-06 coverage and/or WP-10 compliance hosts toward acceptance gate G6.
- WP-07 updates / transactions.
