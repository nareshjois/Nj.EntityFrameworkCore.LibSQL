# Upstream EF Core SQLite baseline

Attributed source baseline for this provider.

| Item | Value |
|------|--------|
| Repository | https://github.com/dotnet/efcore |
| Tag | `v10.0.10` |
| Commit | `db55508a7fbc1535bdb65b85159a8d0d36d6942a` |
| Published EF packages | `10.0.10` |
| Upstream path | `src/EFCore.Sqlite.Core` |
| Local path | `src/Nj.EntityFrameworkCore.LibSql` |
| Import date | 2026-07-15 |

## Reproduce

```bash
# Fetch the recorded tree
git clone --depth 1 --branch v10.0.10 https://github.com/dotnet/efcore.git /tmp/efcore-v10.0.10
# Or: git -C /tmp/efcore fetch --depth 1 origin db55508a7fbc1535bdb65b85159a8d0d36d6942a

# Compare against this repo after mechanical rename
./eng/compare-upstream-sqlite.sh
```

Functional LibSQL / Nelknet edits must land in separate commits after the
mechanical rename. See [provider-service-map.md](provider-service-map.md).
