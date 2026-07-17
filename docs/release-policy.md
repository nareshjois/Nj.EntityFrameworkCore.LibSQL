# Release policy

## Versioning

| Stream | Version form | Meaning |
|--------|--------------|---------|
| Preview | `10.0.0-preview.N` | Public but unfinished; APIs may change |
| Stable | `10.0.x` | Supported on the EF Core 10 line |
| Next EF major | `11.0.x` (future) | New provider major aligned to EF |

- Provider **major/minor track EF Core**.
- Do **not** label experimental builds as stable.
- EF providers generally need a **new build per EF major version**.

Pins: [versions.md](versions.md) and `Directory.Packages.props`.

## Approval

Until a second maintainer joins: sole maintainer (`nareshjois`) **and**
reproducible CI on the tagged commit.

After a second maintainer: prefer **two-maintainer approval** for stable releases.

## NuGet publishing

| Item | Value |
|------|--------|
| Package ID | `Nj.EntityFrameworkCore.LibSql` |
| Publisher | Accounts under `nareshjois` |
| Symbols | `.snupkg` with SourceLink |

Manual publish is fine for early previews — see [releasing.md](releasing.md).
Automate `dotnet nuget push` after NuGet secrets are configured for maintainers.

## Stable backlog

Before labeling a release `10.0.0` (not Preview):

- Extra advertised RIDs / musl only after smoke
- Compliance skip cleanup / documentation
- Package signing as required
- Public API compatibility baseline
- Support window and upgrade notes

## Security releases

Follow [SECURITY.md](../SECURITY.md). Dependency bumps land through tested PRs.
