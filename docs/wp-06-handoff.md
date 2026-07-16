# WP-06 handoff

**Status:** first slice **merged to `main`** (PR #14). Full G6 EF specification
suites **closed in WP-10** ([wp-10-handoff](wp-10-handoff.md)).

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

## Closed in WP-10

- Full G6 EF relational specification host + functional deferred gaps (TPH, glob/hex/substr, compiled query, interceptors).

## Optional follow-up

- Exact `decimal` / .NET Regex parity via Nelknet CreateFunction.

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Query
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~UdfGap
```

## Next

- Preview 2: burn down `C-009` string-translation SQL golden deltas.
