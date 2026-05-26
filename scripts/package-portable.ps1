param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Stopping HoverTranslate if running..."
Get-Process -Name HoverTranslate -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "Building app icon..."
& (Join-Path $root "scripts\build-icon.ps1")

Write-Host "Publishing..."
dotnet publish "HoverTranslate.App\HoverTranslate.App.csproj" -c $Configuration -r $Runtime --self-contained false

$publishDir = Join-Path $root "HoverTranslate.App\bin\$Configuration\net8.0-windows\$Runtime\publish"
$icon = Join-Path $root "HoverTranslate.App\Assets\xuanyi.ico"
if (Test-Path $icon) {
    Copy-Item $icon (Join-Path $publishDir "xuanyi.ico") -Force
}
if (-not (Test-Path $publishDir)) { throw "Publish folder not found: $publishDir" }

$stamp = Get-Date -Format "yyyyMMdd-HHmm"
$outName = "XuanYi-HoverTranslate-portable-$stamp"
$stage = Join-Path $env:TEMP $outName
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

$pubDest = Join-Path $stage "publish"
Copy-Item $publishDir $pubDest -Recurse
Copy-Item (Join-Path $root "config.example.json") $pubDest -Force
$glossary = Join-Path $root "glossary.json"
if (Test-Path $glossary) { Copy-Item $glossary $pubDest -Force }

$bat = Join-Path $pubDest "Start-XuanYi.bat"
"@echo off`r`ncd /d `"%~dp0`"`r`nstart `"`" HoverTranslate.exe`r`n" | Out-File -FilePath $bat -Encoding ASCII

$srcDest = Join-Path $stage "source"
New-Item -ItemType Directory -Path $srcDest -Force | Out-Null
@(
    "HoverTranslate.App",
    "HoverTranslate.Core",
    "scripts",
    "README.md",
    "README-MIGRATION.md",
    "SECURITY.md",
    "HoverTranslate.sln",
    "config.example.json"
) | ForEach-Object {
    $p = Join-Path $root $_
    if (Test-Path $p) {
        $dest = Join-Path $srcDest $_
        if ((Get-Item $p).PSIsContainer) {
            Copy-Item $p $dest -Recurse -Force
        } else {
            Copy-Item $p $dest -Force
        }
    }
}

$zipPath = Join-Path $root "dist\$outName.zip"
New-Item -ItemType Directory -Path (Split-Path $zipPath) -Force | Out-Null
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zipPath -Force

Write-Host "Done: $zipPath"
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
