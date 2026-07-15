param(
  [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
New-Item -ItemType Directory -Force -Path "artifacts/packages" | Out-Null
dotnet pack src/Nj.EntityFrameworkCore.LibSql/Nj.EntityFrameworkCore.LibSql.csproj `
  -c $Configuration `
  -o "$root/artifacts/packages"
Get-ChildItem artifacts/packages
