# Limitations

`Nj.EntityFrameworkCore.LibSql` aims for SQLite-compatible EF Core behavior where
libSQL and the soft-forked Nelknet driver support it. The following are
**explicit non-goals** for this provider.

ADO.NET comes from the git submodule `external/Nelknet.LibSQL`
([nareshjois/Nelknet.LibSQL](https://github.com/nareshjois/Nelknet.LibSQL) soft-fork of
upstream Nelknet). See [adr/0001-soft-fork-nelknet.md](adr/0001-soft-fork-nelknet.md).
Extension / create_function APIs remain unsupported.

## Administration and platform

- Creating, deleting, or administering Turso or self-hosted `sqld` databases,
  namespaces, auth tokens, or backups is **out of scope**. Provision those with
  Turso / `sqld` tooling; the provider only talks to an already-addressable
  database.
- Turso Cloud is **not** required CI coverage. Self-hosted `sqld` is the remote
  contract in CI. You may still use Turso connection strings in applications.

## Unsupported SQLite / native APIs

Do not claim support for SQLite C APIs that Nelknet intentionally does not
expose, including:

- Custom native SQL functions (`sqlite3_create_function` / aggregates) — Nelknet
  does not expose ADO.NET CreateFunction. Decimal LINQ is rewritten to
  REAL/`CAST` (not exact `decimal` precision). `Regex.IsMatch` uses libSQL’s
  native `REGEXP` (PCRE2), not a managed EF SQLite UDF — see
  [udf-gap.md](udf-gap.md).
- Backup API
- Incremental blob I/O
- Loadable extensions (including SpatiaLite loading)
- Authorizer callbacks
- Progress handlers

## Transactions and distributed features

- Distributed transactions are not supported.
- Nested transaction behavior follows Nelknet / libSQL (documented in driver
  contract tests when audited).

## Provider policy

- Do not silently emulate unsupported behavior in memory.
- Do not fork or bundle the full `dotnet/efcore` tree; import only the attributed
  SQLite provider baseline and test against published EF packages.
- Do not copy third-party provider implementations.
- Do not reference `Microsoft.EntityFrameworkCore.Sqlite` (brings
  `Microsoft.Data.Sqlite` and the native SQLite bundle).

## Database create / delete

| Mode | `EnsureCreated` | `EnsureDeleted` |
|------|-----------------|-----------------|
| Local file | May create schema and file (EF SQLite–like) | May delete the database file |
| Remote | Schema only inside an already-addressable database | Always throws `NotSupportedException` |
| Embedded replica | Same as remote for create semantics | Always throws `NotSupportedException` |

## Preview scope

- **Preview 1:** local + remote.
- **Preview 2+:** embedded replicas and an EF `DatabaseFacade` sync API that
  delegates to Nelknet without inventing stronger consistency guarantees.

Feature-level capability tracking lives in [compatibility.md](compatibility.md).
