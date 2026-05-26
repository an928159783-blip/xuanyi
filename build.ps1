# 编译并发布 HoverTranslate
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

dotnet restore
dotnet build -c Release
dotnet publish HoverTranslate.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

Write-Host ""
Write-Host "发布完成:"
Write-Host "  $PSScriptRoot\HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish\HoverTranslate.exe"
