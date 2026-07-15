# Version baseline

Pinned at repository scaffold (2026-07-15). Update this file when bumps land.

| Component | Version |
|-----------|---------|
| .NET SDK (`global.json`) | `10.0.100` (`rollForward: latestFeature`) |
| EF Core (runtime, relational, design, specification tests) | `10.0.10` |
| EF Core SQLite source baseline (`v10.0.10`) | commit `db55508a7fbc1535bdb65b85159a8d0d36d6942a` — see [upstream-baseline.md](upstream-baseline.md) |
| Test framework (our suites) | `xunit.v3` `3.2.2` |
| Test framework (ComplianceTests / EF Spec packages) | `xunit` `2.9.3` |
| `Nelknet.LibSQL.Data` (runtime) | Soft-fork submodule [`external/Nelknet.LibSQL`](../external/Nelknet.LibSQL) → [nareshjois/Nelknet.LibSQL](https://github.com/nareshjois/Nelknet.LibSQL) `@c73baf3` (upstream `0.2.10` + RETURNING drain + HTTP Hrana errors/baton + unprefixed parameter normalize + Close command finalize / ClearAllPools) |
| Bundled libSQL (bindings) | `libsql-server-v0.24.32` — see [driver-contract.md](driver-contract.md) |
| Provider package | `10.0.0-preview.1` |
| `dotnet-ef` tool | `10.0.10` |
| `libsql-server` image (Testcontainers + compose) | `ghcr.io/tursodatabase/libsql-server:ef758d9@sha256:817fb6c6865d048a509f5c120905629fb9b5af20ad0c526cdc68a6d8793898ad` |
| Testcontainers | `4.13.0` |

## Notes

- Keep **all** EF runtime / relational / design / specification-test packages on
  the **identical** patch.
- Record the native libSQL version exposed by Nelknet after dependency bumps
  (see [driver-contract.md](driver-contract.md)).
- Before stable: re-validate against current and previous supported `sqld`
  releases when practical.
- EF Core providers generally require a new build per EF major version.
