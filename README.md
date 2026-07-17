# Nj.EntityFrameworkCore.LibSql

Community [EF Core](https://learn.microsoft.com/ef/core/) 10 provider for
[libSQL](https://docs.turso.tech/libsql), backed by in-repo
[`Nj.LibSql.Data`](src/Nj.LibSql.Data) ([ADR-0002](docs/adr/0002-nj-libsql-data.md)).

> **Status:** early development (`10.0.0-preview.1`). `UseLibSql` works in-repo
> against `Nj.LibSql.Data`. Not published to NuGet.org yet — do not use in
> production until a stable `10.0.x` release.

## Package identity

| Item | Value |
|------|--------|
| NuGet package | `Nj.EntityFrameworkCore.LibSql` |
| Namespace / assembly | `Nj.EntityFrameworkCore.LibSql` |
| Current local version | `10.0.0-preview.1` |
| License | MIT |
| EF Core | `10.0.10` |
| ADO.NET | `Nj.LibSql.Data` + `Nj.LibSql.Bindings` (see [docs/versions.md](docs/versions.md)) |

Public API follows Microsoft’s Sqlite naming pattern (`UseLibSql`,
`AddEntityFrameworkLibSql`, …). Connection modes (local file, remote `sqld` /
Turso, embedded replica) are selected via the connection string — there are no
mode-specific `Use*` helpers.

## Preview modes

| Mode | Preview 1 | Notes |
|------|-----------|--------|
| Local libSQL file | Yes | File create/delete like EF SQLite |
| Remote self-hosted `sqld` / Turso | Yes | Database must already exist |
| Embedded replica | Preview 2+ | Sync API deferred |

## Quick start (development)

```bash
dotnet restore Nj.EntityFrameworkCore.LibSql.slnx
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.UnitTests -c Release
./eng/verify-package.sh
```

Remote `sqld` for integration tests:

```bash
./eng/start-sqld.sh && ./eng/wait-for-sqld.sh
```

## Documentation

- [Docs index](docs/README.md)
- [Connection modes](docs/connection-modes.md)
- [Limitations](docs/limitations.md)
- [Testing](docs/testing.md)
- [Contributing](CONTRIBUTING.md)
- [Security](SECURITY.md)
- [Code of conduct](CODE_OF_CONDUCT.md)

## Repository

- **GitHub:** [nareshjois/Nj.EntityFrameworkCore.LibSQL](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL)
- **Attribution:** [NOTICE](NOTICE)
- **Changelog:** [CHANGELOG.md](CHANGELOG.md)

## License

MIT — see [LICENSE](LICENSE). Imported EF Core SQLite provider source retains
Microsoft’s MIT copyright headers; community modifications are attributed in
[NOTICE](NOTICE).
