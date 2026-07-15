# WP-05 handoff

Branch: `wp-05-type-mapping`

## Done (G5)

- Local + remote EF parameter round-trips for core CLR store types
  (bool, integers, float/double, decimal, string, Guid, temporals, blob,
  enum, nullables, JSON owned + primitive collection).
- Literal `FromSql` filter coverage for selected store string forms.
- Converter key round-trip; create-script assertion for `HasDefaultValue`.
- Nelknet-compatible invariant temporal parameter formats
  (`yyyy-MM-dd HH:mm:ss.fffzzz`, etc.) in type mappings.
- `LibSqlDatabaseCreator.HasTables` opens the connection when needed.
- Differential suite vs `Microsoft.EntityFrameworkCore.Sqlite`
  (`TypeMappingDifferentialTests`; package not referenced from the provider).

## Deferred

- Database-generated keys / `INSERT…RETURNING` under EF SaveChanges
  (Nelknet: reader leaves statements in progress / values do not persist).
  Documented as `C-002`; fix in WP-07.
- Store-default values read back via `RETURNING` (same gap).
- UDF / `ef_*` / `regexp` / `EF_DECIMAL` rewrite (keep fail-fast; `C-001`).
- Full EF BuiltInDataTypes specification host (WP-10).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~TypeMapping
dotnet test test/Nj.EntityFrameworkCore.LibSql.TypeMappingDifferentialTests -c Release
```

## Next

- WP-06 query translation (or WP-07 updates if RETURNING unblocks SaveChanges for generated keys).
