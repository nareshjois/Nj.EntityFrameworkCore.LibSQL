# Nj.EntityFrameworkCore.LibSql

Community [EF Core](https://learn.microsoft.com/ef/core/) 10 provider for [libSQL](https://docs.turso.tech/libsql), backed by [`Nelknet.LibSQL.Data`](https://www.nuget.org/packages/Nelknet.LibSQL.Data).

> **Status:** early scaffolding. Preview packages are not published yet.

## Package identity

| Item | Value |
|------|--------|
| NuGet package | `Nj.EntityFrameworkCore.LibSql` |
| Namespace / assembly | `Nj.EntityFrameworkCore.LibSql` |
| License | MIT |
| Versioning | Aligned to EF Core (`10.0.0-preview.N`, then `10.0.x`) |

Public API follows Microsoft’s Sqlite naming pattern (`UseLibSql`, `AddEntityFrameworkLibSql`, and so on). Connection modes (local file, remote `sqld` / Turso, embedded replica) are selected via the Nelknet connection string — there are no mode-specific `Use*` helpers.

## Preview 1 modes

- Local libSQL database files
- Remote self-hosted `sqld` (and Turso when you supply a connection string)

Embedded replicas are planned for Preview 2+.

## Repository

- **GitHub:** [nareshjois/Nj.EntityFrameworkCore.LibSQL](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL)
- **Attribution:** see [`NOTICE`](NOTICE)
- **Security:** see [`SECURITY.md`](SECURITY.md)

## Development

Scaffolding and build layout land in WP-01 of the project plan. Until then this repository holds governance stubs only.

## License

MIT — see [`LICENSE`](LICENSE). Imported EF Core SQLite provider source retains Microsoft’s MIT copyright headers; community modifications are attributed in [`NOTICE`](NOTICE).
