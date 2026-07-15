# WP-09 handoff

**Status:** first slice on `wp-09-design-time` (FunctionalTests scaffolding
matrix local + remote). Full G9 CLI / golden sample remains later / WP-10.

## Done

- Local + remote Scaffolding matrix via `IDatabaseModelFactory` (S1–S7):
  tables/columns, PK + AUTOINCREMENT (`ValueGenerated.OnAdd`), NOCASE
  collation, unique index, FK, view, `__EFMigrationsHistory` exclusion.
- Design-time DI + codegen smoke (S8–S9): `LibSqlCodeGenerator` emits
  `UseLibSql`; `IDatabaseModelFactory` /
  `IProviderConfigurationCodeGenerator` resolve via
  `LibSqlDesignTimeServices`.
- Folded one-off `ScaffoldingColumnFacetsTests` into the matrix.
- Resilient CLR type inference: remote/sqld `typeof(max(...))` sampling
  failures log a warning and continue; CREATE SQL facets still apply.

## Deferred (not this slice)

- Golden C# entity/DbContext files.
- Virtual tables / vector types policy.
- Full `dotnet ef migrations add` / `dbcontext scaffold` CLI +
  MigrationsSample G9 walkthrough.
- Pluralization edge goldens; migration script generation (documented / throws).

## Verify

```bash
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release --filter FullyQualifiedName~Scaffolding
```

## Next

- Full G9 / MigrationsSample CLI polish, and/or WP-10 compliance.
- Deeper WP-09 coverage (goldens, virtual tables).
