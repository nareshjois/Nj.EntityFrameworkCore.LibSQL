# Migrate from EF Core SQLite

Moving an app from `Microsoft.EntityFrameworkCore.Sqlite` /
`Microsoft.Data.Sqlite` to `Nj.EntityFrameworkCore.LibSql`.

## Package and registration

| Before | After |
|--------|--------|
| `Microsoft.EntityFrameworkCore.Sqlite` | `Nj.EntityFrameworkCore.LibSql` |
| `options.UseSqlite(cs)` | `options.UseLibSql(cs)` |
| `AddDbContext` + UseSqlite | Same + `UseLibSql` (see `samples/DiPoolingSample`) |
| `Microsoft.Data.Sqlite.SqliteConnection` | `Nj.LibSql.Data.LibSqlConnection` |

Do **not** reference both providers in the same project if they fight over
native SQLite loads. Differential tests are the only intentional dual reference.

## Connection strings

- Local file / `:memory:` paths are largely familiar.
- Remote libSQL uses `Data Source=http(s)://…` or `libsql://…` plus
  `Auth Token` — not a Microsoft.Data.Sqlite feature.
- Embedded replica: local `Data Source` + `Sync URL` (see
  [connection-strings.md](connection-strings.md)).

## Behavioral differences

| Area | EF SQLite | This provider |
|------|-----------|---------------|
| Decimal LINQ | Managed `ef_*` / decimal helpers | Rewritten to `REAL` / `CAST` (IEEE double — **not** exact `decimal`) |
| `Regex.IsMatch` | .NET Regex UDF | Native libSQL `REGEXP` (**PCRE2**) |
| SpatiaLite / load_extension | Possible with Microsoft.Data.Sqlite | **Unsupported** (C-006) |
| Custom SQL functions | `CreateFunction` | **Unsupported** |
| Remote DB | N/A | DB must already exist; `EnsureDeleted` throws |
| Embedded Sync | N/A | `Database.Sync` / `LibSqlConnection.Sync` (sqld; Turso **C-019**) |
| Shared-cache dirty reads | Possible with shared cache | No shared-cache; `DirtyReadsOccur => false` (C-008) |

Details: [limitations.md](limitations.md), [compatibility.md](compatibility.md).

## Checklist

1. Swap package references and `UseSqlite` → `UseLibSql`.
2. Re-run migrations against a **copy** of the database; validate decimal/regex
   queries if you relied on EF SQLite UDFs.
3. For Turso / `sqld`, provision the database externally before `Migrate` /
   `EnsureCreated`.
4. Review waivers (C-001, C-004, C-019, …) that apply to your features.
