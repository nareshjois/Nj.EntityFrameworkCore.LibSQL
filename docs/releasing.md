# Releasing

Runbook for shipping `Nj.EntityFrameworkCore.LibSql` / `Nj.LibSql.Data` /
`Nj.LibSql.Bindings` to NuGet.org.

## Preview (`10.0.0-preview.N`)

1. **Green `main`** — CI + integration (sqld) + driver suites as required.
2. **Changelog** — move notes under `[10.0.0-preview.N]` in [CHANGELOG.md](../CHANGELOG.md).
3. **Version** — `Directory.Build.props` `VersionPrefix` / `VersionSuffix`
   match the tag (currently `10.0.0` + `preview.1`).
4. **Tag** (annotated):

   ```bash
   git tag -a v10.0.0-preview.1 -m "Nj.EntityFrameworkCore.LibSql 10.0.0-preview.1"
   git push origin v10.0.0-preview.1
   ```

5. **Pack CI** — tag push runs [`.github/workflows/package.yml`](../.github/workflows/package.yml):
   nupkgs, SBOM, build provenance attestation. Download the `nupkgs` artifact
   (or pack locally: `./eng/pack.sh && ./eng/verify-package.sh`).
6. **Push** (sole maintainer; API key with push permission):

   ```bash
   export NUGET_API_KEY=…   # do not commit
   dotnet nuget push artifacts/packages/*.nupkg \
     --api-key "$NUGET_API_KEY" \
     --source https://api.nuget.org/v3/index.json \
     --skip-duplicate
   ```

7. **Verify** — create a throwaway console app:

   ```bash
   dotnet new console -n PreviewSmoke
   cd PreviewSmoke
   dotnet add package Nj.EntityFrameworkCore.LibSql --prerelease
   # UseLibSql + SELECT 1 / EnsureCreated on a temp file
   ```

8. **GitHub Release** — attach notes from CHANGELOG; link compatibility + C-019.

## Stable backlog (post-preview)

Not required for Preview NuGet; track before labeling `10.0.0`:

- [ ] Advertise additional RIDs only after smoke (`win-arm64`, `linux-arm64`,
      `osx-x64`, musl) — [deployment.md](deployment.md)
- [ ] Drive unexplained compliance skips toward zero / document remaining
- [ ] Package signing (if required by policy)
- [ ] API compatibility baselining for public surface
- [ ] Support window + upgrade/rollback notes
- [ ] Prefer two-maintainer approval when available

See [release-policy.md](release-policy.md) and [SECURITY.md](../SECURITY.md).
