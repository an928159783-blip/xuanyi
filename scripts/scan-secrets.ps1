$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$gitleaks = Get-Command gitleaks -ErrorAction SilentlyContinue
if (-not $gitleaks) {
    Write-Host "[SKIP] gitleaks not installed. Install: winget install gitleaks" -ForegroundColor Yellow
    Write-Host "       Or download from https://github.com/gitleaks/gitleaks/releases"
    exit 0
}

Write-Host "Running gitleaks detect..."
& gitleaks detect --source $root --config (Join-Path $root ".gitleaks.toml") --no-banner
if ($LASTEXITCODE -ne 0) {
    Write-Host "gitleaks found potential secrets." -ForegroundColor Red
    exit 1
}
Write-Host "No secrets detected." -ForegroundColor Green
exit 0
