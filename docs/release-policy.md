# Release policy

## Versioning

| Stream | Version form | Meaning |
|--------|--------------|---------|
| Preview | `10.0.0-preview.N` | Public but unfinished; APIs may change |
| Stable | `10.0.x` | Supported on the EF Core 10 line |
| Next EF major | `11.0.x` (future) | New provider major aligned to EF |

- Provider **major/minor track EF Core**.
- Do **not** label experimental builds as stable.
- EF Core providers generally require a **new build per EF major version**.

Pinned dependency versions for the current line live in
[versions.md](versions.md) and `Directory.Packages.props`.

## Approval

Until a second maintainer joins:

- Releases require the sole maintainer (`nareshjois`) **and** reproducible CI
  provenance (successful package/release workflow on the tagged commit).

After a second maintainer exists:

- Prefer **two-maintainer approval** for stable releases.

## NuGet publishing

| Item | Value |
|------|--------|
| Package ID | `Nj.EntityFrameworkCore.LibSql` |
| Publisher | Accounts with authority under `nareshjois` |
| Reservation | Reserve the ID on first successful publish; do not announce before reservation |
| Symbols | `.snupkg` with SourceLink |

Manual publish is acceptable for early previews. Automate only after NuGet
publish secrets are configured and documented for maintainers.

## Security releases

Follow [SECURITY.md](../SECURITY.md). Dependency bumps for EF Core / Nelknet
land through tested PRs after verification.
