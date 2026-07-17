# Version baseline

Update this file when pins change.

## Compatibility matrix

| Component | Version / pin |
|-----------|----------------|
| Provider packages | `Nj.EntityFrameworkCore.LibSql`, `Nj.LibSql.Data`, `Nj.LibSql.Bindings` → `10.0.0-preview.1` |
| .NET SDK (`global.json`) | `10.0.100` (`rollForward: latestFeature`) |
| EF Core (runtime, relational, design, specification tests) | `10.0.10` |
| `dotnet-ef` | `10.0.10` |
| EF Core SQLite source baseline (`v10.0.10`) | commit `db55508a7fbc1535bdb65b85159a8d0d36d6942a` — [upstream-baseline.md](upstream-baseline.md) |
| `Nj.LibSql.Data` / `Nj.LibSql.Bindings` | In-monorepo; natives in `runtimes/` |
| Bundled libSQL native | `libsql-server-v0.24.32` (`LIBSQL_VERSION`; rebuild [eng/native/README.md](../eng/native/README.md)) |
| Runtime report | `libSQL Version: 0.2.3` / `SQLite Version: 3.45.1` |
| Advertised RIDs (Preview) | `linux-x64`, `osx-arm64`, `win-x64` — [architecture.md](architecture.md) |
| `libsql-server` CI image | `ghcr.io/tursodatabase/libsql-server:ef758d9@sha256:817fb6c6865d048a509f5c120905629fb9b5af20ad0c526cdc68a6d8793898ad` |
| Testcontainers | `4.13.0` |
| Test framework (primary) | `xunit.v3` `3.2.2` |
| Test framework (ComplianceTests / EF Spec) | `xunit` `2.9.3` |

## Notes

- Keep **all** EF runtime / relational / design / specification-test packages on
  the **identical** patch.
- After native bumps, update `LIBSQL_VERSION` and this table.
- Before **stable**: expand advertised RIDs as needed; re-validate against
  current and previous `sqld` when practical.
- EF providers generally need a **new build per EF major version**.
- Publishing: [releasing.md](releasing.md), [release-policy.md](release-policy.md).
