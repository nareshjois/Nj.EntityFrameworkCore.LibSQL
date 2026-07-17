# WP-15 handoff — Preview NuGet (stable track)

## Summary

**G13** docs/samples complete. Preview package stream is
`10.0.0-preview.1`. Stable `10.0.0` remains a destination — see stable backlog
in [releasing.md](releasing.md).

## Compatibility / known gaps

Authoritative matrix: [compatibility.md](compatibility.md). Highlight for
Preview consumers:

- **C-019** — Turso Cloud embedded-replica Sync hang (#24); use sqld for Sync.
- Advertised RIDs only: `linux-x64`, `osx-arm64`, `win-x64`.
- Decimal / regex dialect differences (C-001).

## Publish

1. ~~Tag `v10.0.0-preview.1`~~ — done (`d445ca7`).
2. ~~Pack CI (SBOM + provenance)~~ — green on the tag run.
3. ~~GitHub Release~~ — [v10.0.0-preview.1](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/releases/tag/v10.0.0-preview.1).
4. **nuget.org via [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)**
   (preferred over long-lived API keys) — see [releasing.md](releasing.md):
   - nuget.org policy: owner `nareshjois`, repo `Nj.EntityFrameworkCore.LibSQL`,
     workflow `package.yml`
   - GitHub secret `NUGET_USER` = nuget.org profile username
   - Then: `gh workflow run package.yml -f publish=true` (or push a new `v*` tag)

5. After push: update README status to “on NuGet.org”; verify with
   `dotnet add package Nj.EntityFrameworkCore.LibSql --prerelease`.

## Next

Stable backlog (RIDs, skips, signing, API baseline) after Preview feedback.
