param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.2"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Stopping HoverTranslate if running..."
Get-Process -Name HoverTranslate -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "Publishing $Version ($Configuration, $Runtime)..."
$env:RefreshDesktopShortcut = "false"
dotnet publish "HoverTranslate.App\HoverTranslate.App.csproj" `
    -c $Configuration -r $Runtime --self-contained false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$pub = Get-ChildItem "HoverTranslate.App\bin\$Configuration" -Recurse -Directory -Filter publish |
    Where-Object { Test-Path (Join-Path $_.FullName "HoverTranslate.exe") } |
    Sort-Object { (Get-Item (Join-Path $_.FullName "HoverTranslate.exe")).LastWriteTime } -Descending |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $pub) { throw "Publish folder not found" }

Write-Host "Cleaning retail artifacts from: $pub"
Get-ChildItem $pub -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force

$stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$stampPath = Join-Path $pub "build-stamp.txt"
"XuanYi $Version`r`nBuilt: $stamp`r`n" | Out-File -FilePath $stampPath -Encoding utf8

$zipName = "XuanYi-$Version-win-x64.zip"
$zipPath = Join-Path $root "dist\$zipName"
New-Item -ItemType Directory -Path (Split-Path $zipPath) -Force | Out-Null
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Write-Host "Creating $zipPath ..."
Compress-Archive -Path (Join-Path $pub "*") -DestinationPath $zipPath -Force
Write-Host "Done: $zipPath ($([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB)"
