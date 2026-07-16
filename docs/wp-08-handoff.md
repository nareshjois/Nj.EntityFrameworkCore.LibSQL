# WP-08 handoff

**Status:** first slice **merged to `main`** (PR #16). Full G8 EF specification
suites closed in **WP-10** ([wp-10-handoff](wp-10-handoff.md)).

## Done

- Local + remote Migrations matrix: `EnsureCreated` schema, local
  `EnsureDeleted` (file removed), remote `EnsureDeleted` throws,
  `Database.Migrate` up (Widgets + `__EFMigrationsHistory`), Migrate down to
  `0`, and lock-row release smoke after Migrate.
- Fixed `LibSqlHistoryRepository` migration lock acquire for Nelknet: split
  `INSERT OR IGNORE` from ownership `SELECT` (multi-statement
  `SELECT changes()` returned null via ExecuteScalar).
- Soft-fork Nelknet `@b0a9c51` unchanged this slice.

## Closed in WP-10

- Full G8 EF migration specification host (`LibSqlMigrationsSqlGeneratorTest`).
- Concurrent migrators, lock recovery, extended op matrix, unsupported ops, remote txn migrate, failure/resume, multi-version chain, N→N+1 pin (`MigrationDeferredCases`).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Migrations
```

## Next

- Preview 2: idempotent script policy; MigrationsSample polish.
