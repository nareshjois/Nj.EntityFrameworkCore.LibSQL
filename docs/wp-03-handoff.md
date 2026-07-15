# WP-03 handoff

Branch: `wp-03-sqlite-baseline`

## Done

- Attributed import of `dotnet/efcore` `src/EFCore.Sqlite.Core` (+ Shared helpers)
  from tag `v10.0.10` (`db55508…`).
- Mechanical rename to `Nj.EntityFrameworkCore.LibSql` / `LibSql*` (separate
  commit from import).
- Docs: `upstream-baseline.md`, `provider-service-map.md`, `capabilities.md`.
- NOTICE updated with import provenance.
- `eng/compare-upstream-sqlite.sh` and weekly `upstream-sqlite-watch` workflow.

## Temporary debt (WP-04)

- Still references `Microsoft.Data.Sqlite.Core` so the renamed connection/scaffold
  code compiles. First WP-04 item: replace with Nelknet and drop that package.

## Gate G3

Maintainers approve [provider-service-map.md](provider-service-map.md) before
functional LibSQL edits.

## Verify

```bash
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release
./eng/compare-upstream-sqlite.sh
```

## Next

- WP-04 provider core: `UseLibSql`, DI, Nelknet `IRelationalConnection`.
