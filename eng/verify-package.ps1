$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
New-Item -ItemType Directory -Force -Path "artifacts/packages","artifacts/test-results" | Out-Null

dotnet restore Nj.EntityFrameworkCore.LibSql.slnx
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release --no-restore
# Nj.LibSql.Data may already be on nuget.org under the same version — pack
# Bindings → Data → EF so PackageTests can resolve against this build's graph.
dotnet pack src/Nj.LibSql.Bindings/Nj.LibSql.Bindings.csproj -c Release --no-build -o "$root/artifacts/packages"
dotnet pack src/Nj.LibSql.Data/Nj.LibSql.Data.csproj -c Release --no-build -o "$root/artifacts/packages"
dotnet pack src/Nj.EntityFrameworkCore.LibSql/Nj.EntityFrameworkCore.LibSql.csproj -c Release --no-build -o "$root/artifacts/packages"

# Drop global-cache copies so restore cannot reuse a stale nuspec under the same version.
$nugetPkgs = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME ".nuget/packages" }
foreach ($id in @("nj.entityframeworkcore.libsql","nj.libsql.data","nj.libsql.bindings")) {
  $dir = Join-Path $nugetPkgs $id
  if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
}

dotnet restore test/Nj.EntityFrameworkCore.LibSql.PackageTests/Nj.EntityFrameworkCore.LibSql.PackageTests.csproj `
  -p:VerifyPackedPackage=true --force --force-evaluate `
  --source "$root/artifacts/packages" `
  --source "https://api.nuget.org/v3/index.json"

dotnet test test/Nj.EntityFrameworkCore.LibSql.PackageTests/Nj.EntityFrameworkCore.LibSql.PackageTests.csproj `
  -c Release --no-restore -p:VerifyPackedPackage=true `
  --logger "trx;LogFileName=package-tests.trx" `
  --results-directory "$root/artifacts/test-results"

dotnet run --project samples/LocalSample/LocalSample.csproj -c Release --no-build
Write-Host "Package verification succeeded."
