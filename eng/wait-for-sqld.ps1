param(
  [int]$TimeoutSeconds = 60,
  [string]$Url = $(if ($env:LIBSQL_TEST_URL) { $env:LIBSQL_TEST_URL } else { "http://127.0.0.1:8080" })
)
$ErrorActionPreference = "Stop"
Write-Host "Waiting for sqld at $Url (timeout ${TimeoutSeconds}s)..."
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
  try {
    Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 | Out-Null
    Write-Host "sqld is reachable."
    exit 0
  } catch {
    Start-Sleep -Seconds 1
  }
}
Write-Error "Timed out waiting for sqld at $Url"
exit 1
