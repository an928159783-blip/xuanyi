# XuanYi smoke tests: build, unit tests, publish artifacts, optional process launch
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$fail = 0
function Test-Check([string]$Name, [bool]$Ok, [string]$Detail = "") {
    if ($Ok) { Write-Host "[PASS] $Name" -ForegroundColor Green }
    else { Write-Host "[FAIL] $Name $Detail" -ForegroundColor Red; $script:fail++ }
}

Write-Host "=== Build ===" -ForegroundColor Cyan
dotnet build HoverTranslate.sln -c Release --no-restore 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { dotnet restore; dotnet build HoverTranslate.sln -c Release }
Test-Check "Solution build" ($LASTEXITCODE -eq 0)

Write-Host "=== Unit tests ===" -ForegroundColor Cyan
dotnet test HoverTranslate.Core.Tests\HoverTranslate.Core.Tests.csproj -c Release --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
    dotnet test HoverTranslate.Core.Tests\HoverTranslate.Core.Tests.csproj -c Release
}
Test-Check "Core unit tests" ($LASTEXITCODE -eq 0)

Write-Host "=== NuGet vulnerable packages ===" -ForegroundColor Cyan
$vulnOut = dotnet list package --vulnerable --include-transitive 2>&1 | Out-String
$hasVuln = $vulnOut -match '>\s+\S'
if ($hasVuln) {
    Write-Host $vulnOut -ForegroundColor Yellow
}
Test-Check "No vulnerable NuGet packages" (-not $hasVuln)

Write-Host "=== Publish artifacts ===" -ForegroundColor Cyan
$pub = Join-Path $root "HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish"
@("HoverTranslate.exe", "HoverTranslate.dll", "HoverTranslate.Core.dll", "xuanyi.ico") | ForEach-Object {
    Test-Check "Publish file $_" (Test-Path (Join-Path $pub $_))
}

Write-Host "=== Launcher scripts (ASCII-safe) ===" -ForegroundColor Cyan
$desktop = Join-Path $env:USERPROFILE "Desktop\启动炫译.cmd"
if (Test-Path $desktop) {
    $raw = Get-Content $desktop -Raw -Encoding Default
    Test-Check "Desktop cmd has start exe" ($raw -match 'start\s+""\s+"%EXE%"')
    Test-Check "Desktop cmd no chcp65001" ($raw -notmatch 'chcp\s+65001')
}

Write-Host "=== Process launch (3s) ===" -ForegroundColor Cyan
Get-Process -Name HoverTranslate -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep 1
$exe = Join-Path $pub "HoverTranslate.exe"
if (Test-Path $exe) {
    $p = Start-Process -FilePath $exe -WorkingDirectory $pub -PassThru
    Start-Sleep 3
    $alive = -not $p.HasExited
    Test-Check "Process stays alive 3s" $alive
    if ($alive) { Stop-Process -Id $p.Id -Force }
    $log = Join-Path $env:USERPROFILE ".hover-translate\startup.log"
    Test-Check "startup.log exists" (Test-Path $log)
}

Write-Host "=== Secret scan (gitleaks) ===" -ForegroundColor Cyan
& (Join-Path $root "scripts\scan-secrets.ps1")
if ($LASTEXITCODE -ne 0) { $script:fail++ }

Write-Host ""
if ($fail -eq 0) {
    Write-Host "All smoke checks passed." -ForegroundColor Green
    exit 0
}
Write-Host "$fail check(s) failed." -ForegroundColor Red
exit 1
