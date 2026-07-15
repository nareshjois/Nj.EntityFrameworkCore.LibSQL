# WP-08 handoff

**Status:** first slice on `wp-08-migrations` (FunctionalTests create/delete /
Migrate matrix).

## Done

- Local + remote Migrations matrix: `EnsureCreated` schema, local
  `EnsureDeleted` (file removed), remote `EnsureDeleted` throws,
  `Database.Migrate` up (Widgets + `__EFMigrationsHistory`), Migrate down to
  `0`, and lock-row release smoke after Migrate.
- Fixed `LibSqlHistoryRepository` migration lock acquire for Nelknet: split
  `INSERT OR IGNORE` from ownership `SELECT` (multi-statement
  `SELECT changes()` returned null via ExecuteScalar).
- Soft-fork Nelknet `@b0a9c51` unchanged this slice.

## Deferred (not this slice)

- Concurrent migrators / lock recovery after crash.
- Full SQLite migration op matrix + unsupported ops.
- Idempotent script generation / MigrationsSample polish.
- Preview package N→N+1 chain; design-time `dotnet ef` (WP-09).
- Full G8 EF specification suites (WP-10).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Migrations
```

## Next

- WP-09 design-time / scaffolding, and/or WP-10 compliance toward full G8.
