# WP-07 handoff

**Status:** first slice **merged to `main`** (PR #15). Full G7 EF specification
suites closed in **WP-10** ([wp-10-handoff](wp-10-handoff.md)).

## Done

- Local + remote Updates matrix: CRUD `SaveChanges` (with thin RETURNING SQL
  assert), multi-entity unique violation auto-rollback, explicit commit/rollback,
  optimistic concurrency (`DbUpdateConcurrencyException` + store winner),
  `ExecuteUpdate` / `ExecuteDelete`, and constraint isolation after a prior
  committed row.
- Store outcomes asserted after `ChangeTracker.Clear` / reopening contexts.
- Soft-fork Nelknet `@b0a9c51` assumed for RETURNING + HTTP baton (no submodule
  bump this slice).

## Closed in WP-10

- Full G7 EF update / concurrency / transaction specification suites (`ComplianceTests`).
- Savepoints, busy/locked stress, cancellation, RETURNING+trigger edge, pooled stress (`UpdateDeferredCases`).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Updates
```

## Next

- Preview 2: transport ambiguity matrix; deeper concurrency stress on remote Turso.
