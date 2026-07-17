# Native libSQL FFI builds (`Nj.LibSql.Bindings`)

Phase 1 ships committed RID assets under
`src/Nj.LibSql.Bindings/runtimes/{linux-x64,osx-arm64,win-x64}/native/`
(bootstrapped from the soft-fork pin `libsql-server-v0.24.32`).

## Rebuild / publish

1. Run Actions workflow **libsql-native** (`workflow_dispatch`).
2. Input `libsql_ref` = upstream tag (e.g. `libsql-server-v0.24.32`).
3. With `publish_release=true`, creates/updates GitHub Release
   `native-libsql-v{version}` with zips:
   - `libsql-linux-x64.zip`
   - `libsql-osx-arm64.zip`
   - `libsql-win-x64.zip`
4. With `commit_runtimes=true`, refreshes committed `runtimes/**` + `LIBSQL_VERSION`.

## Consume in Bindings

- Default `BuildType=ManagedOnly`: uses committed `runtimes/**` (no network).
- `BuildType=Full`: runs `DownloadNativeLibs` against
  `LibSqlNativeArtifactRoot` (default release download URL).

Bump `LibSqlNativeVersion` in `Nj.LibSql.Bindings.csproj` / `docs/versions.md`
when publishing a new native tag.
