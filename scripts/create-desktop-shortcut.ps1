# Publish latest build and refresh desktop shortcut (炫译.lnk -> publish\HoverTranslate.exe)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$pub = Join-Path $root "HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish"
$exe = Join-Path $pub "HoverTranslate.exe"
$ico = Join-Path $pub "xuanyi.ico"

Write-Host "Publishing HoverTranslate (Release, win-x64)..."
Push-Location $root
try {
    dotnet publish HoverTranslate.App\HoverTranslate.App.csproj -c Release -r win-x64 --self-contained false
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

if (-not (Test-Path $exe)) {
    Write-Error "Publish succeeded but exe not found: $exe"
}

$desktop = [Environment]::GetFolderPath("Desktop")
$wsh = New-Object -ComObject WScript.Shell

Get-ChildItem $desktop -Filter "*.lnk" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $s = $wsh.CreateShortcut($_.FullName)
        if ($s.TargetPath -like "*HoverTranslate.exe*") {
            Remove-Item $_.FullName -Force
            Write-Host ("Removed old shortcut: " + $_.Name)
        }
    } catch { }
}

$iconPath = if (Test-Path $ico) { "$ico,0" } else { "$exe,0" }
$shortcutPath = Join-Path $desktop (([char]0x70AB).ToString() + ([char]0x8BD1).ToString() + ".lnk")
$sc = $wsh.CreateShortcut($shortcutPath)
$sc.TargetPath = $exe
$sc.WorkingDirectory = $pub
$sc.Description = 'XuanYi HoverTranslate (latest publish)'
$sc.IconLocation = $iconPath
$sc.Save()

$ver = (Get-Item $exe).LastWriteTime.ToString('yyyy-MM-dd HH:mm')
Write-Host "Created: $shortcutPath"
Write-Host "Target:  $exe"
Write-Host "Built:   $ver"
