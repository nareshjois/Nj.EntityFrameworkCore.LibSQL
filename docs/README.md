# Docs

Current-state documentation for `Nj.EntityFrameworkCore.LibSql` and the in-repo
ADO.NET driver (`Nj.LibSql.Data` / `Nj.LibSql.Bindings`).

## Using the provider

| Document | Purpose |
|----------|---------|
| [architecture.md](architecture.md) | Packages, naming, natives, HTTP/WSS, CI |
| [connection-modes.md](connection-modes.md) | Local, remote, and embedded-replica connection strings |
| [observability.md](observability.md) | ActivitySource spans and redaction notes |
| [deployment.md](deployment.md) | Advertised RIDs, FDD/SC/single-file/container smoke |
| [performance.md](performance.md) | Benchmark baselines and soft regression thresholds |
| [limitations.md](limitations.md) | Explicit non-goals and unsupported APIs |
| [compatibility.md](compatibility.md) | Capability matrix and waiver log |
| [migrations.md](migrations.md) | Migrations and EnsureCreated/Deleted policy |

## Developing

| Document | Purpose |
|----------|---------|
| [testing.md](testing.md) | How to run tests, Testcontainers, Turso secrets |
| [versions.md](versions.md) | Pinned EF / driver / SDK / image versions |
| [upstream-baseline.md](upstream-baseline.md) | EF Core SQLite import provenance |
| [release-policy.md](release-policy.md) | Versioning and NuGet publishing |

Root: [README](../README.md), [CONTRIBUTING](../CONTRIBUTING.md),
[SECURITY](../SECURITY.md), [CODE_OF_CONDUCT](../CODE_OF_CONDUCT.md),
[NOTICE](../NOTICE), [CHANGELOG](../CHANGELOG.md).
