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

- Database-generated keys / `INSERT…RETURNING` under EF SaveChanges —
  documented as `C-002`. Soft-fork follow-up: PR #9 (`soft-fork-nelknet`).
- UDF / `ef_*` / `regexp` / `EF_DECIMAL` rewrite (keep fail-fast; `C-001`).
- Full EF BuiltInDataTypes specification host (WP-10).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~TypeMapping
dotnet test test/Nj.EntityFrameworkCore.LibSql.TypeMappingDifferentialTests -c Release
```

## Next

- Merge this PR (#8), then soft-fork (#9).
- WP-06 query translation.
