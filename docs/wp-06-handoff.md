# WP-06 handoff

**Status:** first slice **merged to `main`** (PR #14). Full G6 EF specification
suites remain WP-10.

## Done

- Local + remote LINQ matrix covering filter/project, order/page, join, include,
  group/aggregate (int), union, strings, math, DateTime members, Guid/bytes,
  JSON owned + primitive collections, `FromSqlInterpolated`, sync/async parity,
  and `TagWith` (thin SQL capture via `LogTo`).
- Soft-fork Nelknet `@b0a9c51`: normalize unprefixed parameter names so EF
  `FromSqlInterpolated` (`p0` vs `@p0`) binds (with prior RETURNING + HTTP baton
  patches).
- **C-001 decimal rewrite:** map negate/arith/compare/mod, OrderBy/ThenBy, and
  Average/Sum/Min/Max to REAL/`CAST` (IEEE precision; see [udf-gap.md](udf-gap.md)).
- **C-001 regex:** `Regex.IsMatch` → native libSQL `REGEXP` / PCRE2 (no
  CreateFunction); engine differs from `System.Text.RegularExpressions`.

## Deferred (not this slice)

- Full G6 EF relational / SQLite specification suites (WP-10 fixtures).
- TPH inheritance, compiled queries/models, Glob/Hex/Substring goldens,
  interceptor suites.
- Exact `decimal` / .NET Regex parity via Nelknet CreateFunction (optional).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Query
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~UdfGap
```

## Next

- **WP-09** design-time / scaffolding (or deeper WP-08).
- Deeper WP-06/WP-07 coverage and/or WP-10 compliance toward full G6/G7.
