# Version baseline

Pinned at repository scaffold (2026-07-15). Update this file when bumps land.

| Component | Version |
|-----------|---------|
| .NET SDK (`global.json`) | `10.0.100` (`rollForward: latestFeature`) |
| EF Core (runtime, relational, design, specification tests) | `10.0.10` |
| `Nelknet.LibSQL.Data` | `0.2.10` (exact pin) |
| Provider package | `10.0.0-preview.1` |
| `dotnet-ef` tool | `10.0.10` |
| `libsql-server` CI image | `ghcr.io/tursodatabase/libsql-server:ef758d9@sha256:817fb6c6865d048a509f5c120905629fb9b5af20ad0c526cdc68a6d8793898ad` |

## Notes

- Keep **all** EF runtime / relational / design / specification-test packages on
  the **identical** patch.
- Record the native libSQL version exposed by Nelknet after the driver contract
  audit (report version string / binding package version in that PR).
- Before stable: re-validate against current and previous supported `sqld`
  releases when practical.
- EF Core providers generally require a new build per EF major version.
