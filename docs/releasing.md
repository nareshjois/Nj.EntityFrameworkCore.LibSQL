# Releasing

Runbook for shipping `Nj.EntityFrameworkCore.LibSql` / `Nj.LibSql.Data` /
`Nj.LibSql.Bindings` to NuGet.org.

## One-time: Trusted Publishing setup

Prefer [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
(OIDC → short-lived API key) over long-lived NuGet API keys.

1. On [nuget.org → Trusted Publishing](https://www.nuget.org/account/trustedpublishing),
   add a policy owned by your nuget.org account (or org):
   - **Repository Owner:** `nareshjois`
   - **Repository:** `Nj.EntityFrameworkCore.LibSQL`
   - **Workflow File:** `package.yml` (file name only)
   - **Environment:** leave empty (workflow does not use a GitHub Environment)
2. In GitHub → Settings → Secrets → Actions, add **`NUGET_USER`**: your
   nuget.org **profile username** (not email). Used by `NuGet/login@v1`.
3. First successful publish permanently activates the policy (IDs from GitHub’s
   token). Private-repo policies may be temporarily active for 7 days until then.

## Preview (`10.0.0-preview.N`)

1. **Green `main`** — CI + integration (sqld) + driver suites as required.
2. **Changelog** — move notes under `[10.0.0-preview.N]` in [CHANGELOG.md](../CHANGELOG.md).
3. **Version** — `Directory.Build.props` `VersionPrefix` / `VersionSuffix`
   match the tag (currently `10.0.0` + `preview.2`).
4. **Tag** (annotated):

   ```bash
   git tag -a v10.0.0-preview.2 -m "Nj.EntityFrameworkCore.LibSql 10.0.0-preview.2"
   git push origin v10.0.0-preview.2
   ```

5. **Pack + publish CI** — tag push runs
   [`.github/workflows/package.yml`](../.github/workflows/package.yml):
   nupkgs, SBOM, provenance, then OIDC login + `dotnet nuget push`.

   To republish without a new tag (e.g. after fixing Trusted Publishing setup):

   ```bash
   gh workflow run package.yml -f publish=true
   ```

6. **Verify** — create a throwaway console app:

   ```bash
   dotnet new console -n PreviewSmoke
   cd PreviewSmoke
   dotnet add package Nj.EntityFrameworkCore.LibSql --prerelease
   # UseLibSql + SELECT 1 / EnsureCreated on a temp file
   ```

7. **GitHub Release** — attach notes from CHANGELOG; link compatibility + C-019.

### Fallback: manual API key

Only if Trusted Publishing is unavailable:

```bash
export NUGET_API_KEY=…   # do not commit
dotnet nuget push artifacts/packages/*.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

## Stable backlog (post-preview)

Not required for Preview NuGet; track before labeling `10.0.0`:

- [ ] Advertise additional RIDs only after smoke (`win-arm64`, `linux-arm64`,
      `osx-x64`, musl) — deferred; Preview keeps three RIDs — [deployment.md](deployment.md)
- [ ] Drive unexplained compliance skips toward zero / document remaining
- [ ] Package signing (if required by policy)
- [ ] API compatibility baselining for public surface
- [ ] Support window + upgrade/rollback notes
- [ ] Prefer two-maintainer approval when available

See [release-policy.md](release-policy.md) and [SECURITY.md](../SECURITY.md).
