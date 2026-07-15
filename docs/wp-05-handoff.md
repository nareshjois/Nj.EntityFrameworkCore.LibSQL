# WP-05 handoff

**Status:** complete on `main` (G5 — PR #8; soft-fork follow-ups PR #10 / #12).

## Done (G5)

- Local + remote EF parameter round-trips for core CLR store types
  (bool, integers, float/double, decimal, string, Guid, temporals, blob,
  enum, nullables, JSON owned + primitive collection).
- Store-generated integer keys via `INSERT…RETURNING` in type-mapping
  round-trips (local + remote) and `GeneratedKeySaveChangesTests`.
- Literal `FromSql` filter coverage for selected store string forms.
- Converter key round-trip; create-script assertion for `HasDefaultValue`.
- Nelknet-compatible invariant temporal parameter formats
  (`yyyy-MM-dd HH:mm:ss.fffzzz`, etc.) in type mappings.
- `LibSqlDatabaseCreator.HasTables` opens the connection when needed.
- Differential suite vs `Microsoft.EntityFrameworkCore.Sqlite`
  (`TypeMappingDifferentialTests`; package not referenced from the provider).
- Soft-fork Nelknet @ `01a8f52`: RETURNING reader drain + HTTP Hrana top-level
  errors + baton-backed streams (ADR-0001). Upstream NuGet PR pending.

## Deferred (not G5 blockers)

- UDF / `ef_*` / `regexp` / `EF_DECIMAL` rewrite (keep fail-fast; `C-001`).
- Full EF BuiltInDataTypes specification host (WP-10).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~TypeMapping
dotnet test test/Nj.EntityFrameworkCore.LibSql.TypeMappingDifferentialTests -c Release
```

## Next

- WP-06 query translation.
