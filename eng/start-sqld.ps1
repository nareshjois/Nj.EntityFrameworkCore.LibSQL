param(
  [int]$TimeoutSeconds = 60
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location (Join-Path $root "eng/sqld")
docker compose up -d
Write-Host "sqld starting on http://127.0.0.1:8080"
