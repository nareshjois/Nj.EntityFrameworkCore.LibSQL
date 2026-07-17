# Limitations

Explicit non-goals for `Nj.EntityFrameworkCore.LibSql`. Feature-level tracking
lives in [compatibility.md](compatibility.md).

ADO.NET comes from in-repo `Nj.LibSql.Data` / `Nj.LibSql.Bindings`
([architecture.md](architecture.md)).

## Administration

- Creating, deleting, or administering Turso / `sqld` databases, namespaces,
  tokens, or backups is **out of scope**. Provision those externally; the
  provider only talks to an already-addressable database.

## Unsupported native APIs

`Nj.LibSql.Data` does not expose:

- Custom SQL functions / aggregates (`CreateFunction`)
- Backup API, incremental blob I/O
- Loadable extensions (including SpatiaLite)
- Authorizer callbacks, progress handlers

### Decimal and regex (vs EF SQLite)

Microsoft EF SQLite registers managed helpers (`ef_*`, `EF_DECIMAL`, regexp UDF).
This provider does not:

| Feature | Behavior |
|---------|----------|
| Decimal LINQ (`+` `-` `*` `/` `%`, compare, aggregates, order) | Rewritten to SQLite `REAL` / `CAST` (IEEE double — **not** exact `System.Decimal`) |
| `Regex.IsMatch` | Native libSQL `REGEXP` / `regexp()` (**PCRE2**, not `System.Text.RegularExpressions`) |

See waiver **C-001** in [compatibility.md](compatibility.md).

## Transactions

- Distributed transactions are not supported.
- Nested / savepoint behavior follows the driver and libSQL.

## Provider policy

- Do not silently emulate unsupported behavior in memory.
- Do not fork the full `dotnet/efcore` tree; import only the attributed SQLite
  baseline and test against published EF packages.
- Do not copy third-party provider implementations.
- Do not reference `Microsoft.EntityFrameworkCore.Sqlite` (pulls
  `Microsoft.Data.Sqlite` and the native SQLite bundle).

## Database create / delete

| Mode | `EnsureCreated` | `EnsureDeleted` |
|------|-----------------|-----------------|
| Local file | May create schema and file | Deletes file when possible; on Windows lock (`C-005`) wipe + tombstone |
| Remote | Schema only inside existing DB | Always throws `NotSupportedException` |
| Embedded replica | Same as remote for create | Always throws `NotSupportedException` |

## Preview scope

- **Preview 1:** local + remote.
- **Preview 2+:** embedded replicas and EF sync API that delegates to the driver.

## Scaffolding

- Virtual tables are not reverse-engineered (`C-004`).
- libSQL vector / `FLOAT32` types have no first-class CLR mapping in Preview 1.
