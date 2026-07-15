# Security Policy

## Supported versions

Security fixes are considered for supported preview and stable releases of
`Nj.EntityFrameworkCore.LibSql` that track the current EF Core 10 line.

| Version | Supported |
|---------|-----------|
| `10.0.0-preview.*` | Yes, while the preview stream is active |
| `10.0.x` (stable) | Yes, once published |
| Older majors | No — providers generally require a new build per EF major |

See [docs/versions.md](docs/versions.md) for the currently pinned EF / Nelknet
versions.

## Reporting a vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

### Preferred: GitHub private vulnerability reporting

1. Open
   https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/security/advisories/new
2. Include:
   - Affected package version(s)
   - Environment (local file / remote `sqld` / Turso)
   - Minimal reproduction
   - Impact assessment
3. Do **not** include production connection strings, auth tokens, or customer
   data.

### Fallback

If private reporting is unavailable, contact the maintainer privately via
[GitHub](https://github.com/nareshjois) (subject: `Security vulnerability`).

Expect an acknowledgment when feasible. Advisories and patched releases will be
coordinated by the maintainer; please allow reasonable time before any public
disclosure.

## Scope notes

- Creating, deleting, or administering Turso / `sqld` databases, namespaces,
  tokens, or backups is **out of scope** for this provider.
- Dependency vulnerabilities in EF Core, Nelknet.LibSQL.Data, or native libSQL
  bindings should also be reported upstream when applicable. This project will
  track and bump pinned versions after verification.

## Disclosure and release approval

Until a second maintainer joins:

- Security and package releases require the **sole maintainer** (`nareshjois`)
  plus **reproducible CI provenance** (successful release/package workflow on a
  tagged commit).

After a second maintainer exists, prefer **two-maintainer approval** for stable
releases.

NuGet publishing for `Nj.EntityFrameworkCore.LibSql` is limited to accounts with
explicit publishing authority under `nareshjois`. The package ID should be
reserved before the first public announcement.
