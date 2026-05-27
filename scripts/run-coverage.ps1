$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$outDir = Join-Path $root "reports\coverage"
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Write-Host "Running tests with coverage..."
dotnet test HoverTranslate.Core.Tests\HoverTranslate.Core.Tests.csproj -c Release `
    --collect:"XPlat Code Coverage" `
    --results-directory $outDir

$coverageFile = Get-ChildItem -Path $outDir -Recurse -Filter "coverage.cobertura.xml" | Select-Object -First 1
if (-not $coverageFile) {
    Write-Host "coverage.cobertura.xml not found under $outDir" -ForegroundColor Red
    exit 1
}

Write-Host "Coverage file: $($coverageFile.FullName)"

$reportGen = Get-Command reportgenerator -ErrorAction SilentlyContinue
if ($reportGen) {
    $htmlDir = Join-Path $outDir "html"
    reportgenerator -reports:$coverageFile.FullName -targetdir:$htmlDir -reporttypes:HtmlSummary
    Write-Host "HTML report: $htmlDir\index.html"
}

# Parse line-rate from cobertura for HoverTranslate.Core
[xml]$xml = Get-Content $coverageFile.FullName
$core = $xml.coverage.packages.package | Where-Object { $_.name -like '*HoverTranslate.Core*' } | Select-Object -First 1
if ($core) {
    $rate = [double]$core.'line-rate' * 100
    Write-Host ("HoverTranslate.Core line coverage: {0:N1}%" -f $rate) -ForegroundColor Cyan
    if ($rate -lt 70) {
        Write-Host "Below 70% target — see 工作计划/P1-质量与安全/覆盖率基线.md" -ForegroundColor Yellow
    }
}

exit 0
