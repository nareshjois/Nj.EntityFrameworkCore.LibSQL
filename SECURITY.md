# Security Policy

## Supported versions

Security fixes are considered for supported preview and stable releases of
`Nj.EntityFrameworkCore.LibSql` that track a current EF Core 10 line.

| Version | Supported |
|---------|-----------|
| `10.0.0-preview.*` | Yes, while the preview stream is active |
| `10.0.x` (stable) | Yes, once published |
| Older majors | No (providers generally require a new build per EF major) |

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Use GitHub’s private vulnerability reporting for this repository:

1. Open https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/security/advisories/new
2. Include: affected package version, environment (local / remote `sqld`),
   minimal reproduction, and impact assessment.
3. Do **not** include production connection strings, auth tokens, or customer data.

If private reporting is unavailable, contact the maintainer privately via the
email listed on the [GitHub profile](https://github.com/nareshjois) security /
contact details.

Expect an acknowledgment when feasible. Advisories and patched releases will be
coordinated by the maintainer; please allow reasonable time before any public
disclosure.

## Scope notes

- Creating, deleting, or administering Turso / `sqld` databases, namespaces,
  tokens, or backups is **out of scope** for this provider.
- Dependency vulnerabilities in EF Core, Nelknet.LibSQL.Data, or native libSQL
  bindings should also be reported upstream when applicable; we will track and
  bump pinned versions after verification.

## Release approval

Until a second maintainer joins:

- Releases require the **sole maintainer** (`nareshjois`) plus **reproducible CI
  provenance** (successful release workflow on a tagged commit).
- After a second maintainer exists, prefer **two-maintainer approval** for
  stable releases.

NuGet publishing for `Nj.EntityFrameworkCore.LibSql` is reserved to accounts
with explicit publishing authority under `nareshjois`. The package ID should be
reserved before the first public announcement.
