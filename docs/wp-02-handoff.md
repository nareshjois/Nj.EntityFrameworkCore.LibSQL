# WP-02 handoff

Branch: `wp-02-driver-contract`

## Done

- Local Nelknet ADO.NET contract suite (lifecycle, commands/parameters, transactions,
  error shapes, `LibSQLFactory`).
- Remote tests gated by Docker/Testcontainers (or `LIBSQL_TEST_URL`); see
  `docs/driver-contract.md`.
- Integration workflow runs the driver suite; sqld is started via Testcontainers.

## Deferred / documented gaps

- Embedded replica mode (Preview 2+).
- Mid-commit network fault injection.
- Turso Cloud auth/TLS matrix.

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.DriverContractTests -c Release
```

## Next

- WP-03 EF SQLite baseline import (parallel-safe with remaining driver gaps).
- Provider core (WP-04) can begin once WP-03 + this contract narrative are reviewed.
