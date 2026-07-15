# Upstream EF Core SQLite baseline

Attributed source baseline for this provider.

| Item | Value |
|------|--------|
| Repository | https://github.com/dotnet/efcore |
| Tag | `v10.0.10` |
| Commit | `db55508a7fbc1535bdb65b85159a8d0d36d6942a` |
| Published EF packages | `10.0.10` |
| Upstream path | `src/EFCore.Sqlite.Core` (+ `src/Shared/*.cs` helpers compiled into the provider) |
| Local path | `src/Nj.EntityFrameworkCore.LibSql` |
| Import date | 2026-07-15 |

## Commit hygiene

1. **Import** — Microsoft namespaces/type names preserved.
2. **Mechanical rename** — `Sqlite` → `LibSql`, namespaces → `Nj.EntityFrameworkCore.LibSql*`.
3. **Functional LibSQL / Nelknet edits** — separate commits after G3 approval (WP-04+).

## Reproduce / compare

```bash
./eng/compare-upstream-sqlite.sh
```

The script sparse-checkouts the recorded tag, reverse-maps local `LibSql` identifiers
to `Sqlite` for comparison, and writes a report under `artifacts/upstream-diff/`.

Weekly CI: `.github/workflows/upstream-sqlite-watch.yml` looks for newer `v10.0.*`
tags than the pin above.

## Temporary dependency

Compile currently references `Microsoft.Data.Sqlite.Core` (not
`Microsoft.EntityFrameworkCore.Sqlite`) so the imported connection/scaffold code
builds. Removing that package and wiring Nelknet is the first WP-04 item — see
[provider-service-map.md](provider-service-map.md).
