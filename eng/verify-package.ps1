$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
New-Item -ItemType Directory -Force -Path "artifacts/packages","artifacts/test-results" | Out-Null

dotnet restore Nj.EntityFrameworkCore.LibSql.slnx
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release --no-restore
dotnet pack src/Nj.EntityFrameworkCore.LibSql/Nj.EntityFrameworkCore.LibSql.csproj -c Release --no-build -o "$root/artifacts/packages"

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
