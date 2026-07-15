# WP-07 handoff

**Status:** first slice on `wp-07-updates-transactions` (FunctionalTests update /
transaction matrix).

## Done

- Local + remote Updates matrix: CRUD `SaveChanges` (with thin RETURNING SQL
  assert), multi-entity unique violation auto-rollback, explicit commit/rollback,
  optimistic concurrency (`DbUpdateConcurrencyException` + store winner),
  `ExecuteUpdate` / `ExecuteDelete`, and constraint isolation after a prior
  committed row.
- Store outcomes asserted after `ChangeTracker.Clear` / reopening contexts.
- Soft-fork Nelknet `@b0a9c51` assumed for RETURNING + HTTP baton (no submodule
  bump this slice unless a case forces it).

## Deferred (not this slice)

- Savepoints nested under user transactions.
- Busy/locked, cancellation/timeout, ambiguous transport failure.
- RETURNING fallbacks for triggers / virtual tables.
- Full G7 EF update / concurrency / transaction specification suites (WP-10).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Updates
```

## Next

- WP-08 migrations / locking, and/or WP-10 compliance toward full G7.
