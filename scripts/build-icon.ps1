$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$ico = Join-Path $root "HoverTranslate.App\Assets\xuanyi.ico"
New-Item -ItemType Directory -Force -Path (Split-Path $ico) | Out-Null
dotnet run --project (Join-Path $root "tools\IconBuilder\IconBuilder.csproj") -c Release -- --brand $ico
Write-Host "Icon: $ico"
