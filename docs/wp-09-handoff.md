# WP-09 handoff

**Status:** G9 closed on `wp-09-g9-complete` (preview matrix already on `main`
via PR #17; this slice finishes CLI sample, goldens, script policy, and
virtual/vector scaffolding policy).

## Done

- Preview matrix S1–S9 (factory + design DI) — PR #17.
- [`samples/MigrationsSample`](../samples/MigrationsSample): Blog/Post model,
  `IDesignTimeDbContextFactory`, checked-in `InitialCreate`, seed schema,
  README walkthrough for migrate / scaffold / scripts.
- [`eng/verify-migrations-sample.sh`](../eng/verify-migrations-sample.sh):
  `database update`, run, non-idempotent script, idempotent failure, scaffold.
- Golden tests: `IReverseEngineerScaffolder` emits `UseLibSql` + entities;
  `GenerateCreateScript` DDL fragment; idempotent history helpers throw;
  virtual tables excluded from `DatabaseModel`.
- Factory skips `CREATE VIRTUAL TABLE` (C-004); vector catalog still denylisted.

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Scaffolding
./eng/verify-migrations-sample.sh
# optional remote Path A:
# LIBSQL_CONNECTION='Data Source=http://127.0.0.1:8080' ./eng/verify-migrations-sample.sh --remote
```

## Next

- Preview 2: embedded replica, expanded remote matrix — see [wp-10-handoff](wp-10-handoff.md) out-of-scope list.
