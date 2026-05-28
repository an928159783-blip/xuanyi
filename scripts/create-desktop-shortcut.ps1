# Publish latest build and refresh desktop shortcut (炫译.lnk -> publish\HoverTranslate.exe)
param(
    [switch]$SkipPublish,
    [switch]$NoKill
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Get-LatestPublishDir {
    $releaseRoot = Join-Path $root "HoverTranslate.App\bin\Release"
    if (-not (Test-Path $releaseRoot)) { return $null }
    $dirs = Get-ChildItem $releaseRoot -Directory -Recurse -Filter publish -ErrorAction SilentlyContinue |
        Where-Object { Test-Path (Join-Path $_.FullName "HoverTranslate.exe") }
    if (-not $dirs) { return $null }
    $dirs |
        Sort-Object { (Get-Item (Join-Path $_.FullName "HoverTranslate.exe")).LastWriteTime } -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

function Get-DesktopFolders {
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($p in @(
            [Environment]::GetFolderPath("Desktop"),
            (Join-Path $env:USERPROFILE "Desktop"),
            (Join-Path $env:USERPROFILE "OneDrive\Desktop"),
            (Join-Path $env:USERPROFILE "OneDrive\桌面")
        )) {
        if ([string]::IsNullOrWhiteSpace($p)) { continue }
        if (Test-Path $p) { [void]$set.Add((Resolve-Path $p).Path) }
    }
    $set
}

if (-not $NoKill) {
    $procs = Get-Process -Name "HoverTranslate" -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "Stopping running HoverTranslate (required to overwrite publish files)..."
        $procs | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
}

if (-not $SkipPublish) {
    Write-Host "Publishing HoverTranslate (Release, win-x64)..."
    Push-Location $root
    try {
        dotnet publish HoverTranslate.App\HoverTranslate.App.csproj -c Release -r win-x64 --self-contained false
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    }
    finally {
        Pop-Location
    }
}

$pub = Get-LatestPublishDir
if (-not $pub) {
    $pub = Join-Path $root "HoverTranslate.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
}
$exe = Join-Path $pub "HoverTranslate.exe"
$ico = Join-Path $pub "xuanyi.ico"

if (-not (Test-Path $exe)) {
    Write-Error "HoverTranslate.exe not found: $exe`nRun without -SkipPublish or fix the build."
}

$builtAt = (Get-Item $exe).LastWriteTime
$stamp = $builtAt.ToString("yyyy-MM-dd HH:mm:ss")
Set-Content -Path (Join-Path $pub "build-stamp.txt") -Value $stamp -Encoding UTF8

# 移除已废弃的旧 publish 目录，避免误点旧 exe
$legacyPub = Join-Path $root "HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish"
if ((Test-Path $legacyPub) -and ($legacyPub -ne $pub)) {
    Write-Host "Removing legacy publish folder (outdated TFM path): $legacyPub"
    Remove-Item $legacyPub -Recurse -Force -ErrorAction SilentlyContinue
}

$wsh = New-Object -ComObject WScript.Shell
$iconPath = if (Test-Path $ico) { "$ico,0" } else { "$exe,0" }
$lnkName = ([char]0x70AB).ToString() + ([char]0x8BD1).ToString() + ".lnk"
$desc = "XuanYi HoverTranslate | built $stamp"

$created = @()
foreach ($desktop in Get-DesktopFolders) {
    Get-ChildItem $desktop -Filter "*.lnk" -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $s = $wsh.CreateShortcut($_.FullName)
            if ($s.TargetPath -like "*HoverTranslate.exe*") {
                Remove-Item $_.FullName -Force
                Write-Host "Removed old shortcut: $($_.FullName)"
            }
        }
        catch { }
    }

    $shortcutPath = Join-Path $desktop $lnkName
    $sc = $wsh.CreateShortcut($shortcutPath)
    $sc.TargetPath = $exe
    $sc.WorkingDirectory = $pub
    $sc.Description = $desc
    $sc.IconLocation = $iconPath
    $sc.Save()
    $created += $shortcutPath
}

Write-Host ""
Write-Host "OK Desktop shortcut refreshed."
foreach ($p in $created) { Write-Host "  Lnk: $p" }
Write-Host "  Exe: $exe"
Write-Host "  Built: $stamp"
Write-Host ""
Write-Host "Verify: open publish\build-stamp.txt or read shortcut Comment (built ...)."
Write-Host "If features look old: tray -> Exit XuanYi, then start again (running process does NOT reload new exe)."
