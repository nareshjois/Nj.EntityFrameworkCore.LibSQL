# WP-04 handoff

Branch: `wp-04-provider-core`

## Done (G4)

- `UseLibSql` with Nelknet `LibSQLConnection` (connection string + existing connection).
- `LibSqlRelationalConnection` / `LibSqlDatabaseCreator` no longer use
  `Microsoft.Data.Sqlite`.
- SpatiaLite / `UseSpatialite` fail loudly (`NotSupportedException`).
- Server-version gates use `LibSqlDatabaseCapabilities` (SQLite 3.45.1 baseline).
- Functional smoke: local + remote `SELECT 1`; `AddDbContext` /
  `AddDbContextFactory` / `AddPooledDbContextFactory`.
- UDF gap catalog (`docs/udf-gap.md`) + fail-fast translation for `ef_*` /
  `regexp` / `EF_DECIMAL` (Nelknet has no CreateFunction).

## Deferred

- Design-time scaffolding native metadata (`sqlite3_table_column_metadata`).
- Full CRUD / migrations suites (WP-05+ / WP-07).
- Embedded replica sync API (Preview 2+).
- Restore or rewrite `ef_*` / `regexp` / `EF_DECIMAL` after Nelknet UDF APIs
  or WP-05 translation changes ([udf-gap.md](udf-gap.md)).

## Verify

```bash
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release
```

## Next

- WP-05 type mapping / SQL generation round-trips.
