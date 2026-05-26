$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$pub = Join-Path $root "HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish"
$exe = Join-Path $pub "HoverTranslate.exe"
$ico = Join-Path $pub "xuanyi.ico"

if (-not (Test-Path $exe)) {
    Write-Host "Publish first."
    exit 1
}

$desktop = [Environment]::GetFolderPath("Desktop")
$wsh = New-Object -ComObject WScript.Shell

Get-ChildItem $desktop -Filter "*.lnk" -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $s = $wsh.CreateShortcut($_.FullName)
        if ($s.TargetPath -like "*HoverTranslate.exe*") {
            Remove-Item $_.FullName -Force
            Write-Host ("Removed: " + $_.Name)
        }
    } catch { }
}

$iconPath = if (Test-Path $ico) { "$ico,0" } else { "$exe,0" }
$shortcutPath = Join-Path $desktop (([char]0x70AB).ToString() + ([char]0x8BD1).ToString() + ".lnk")
$sc = $wsh.CreateShortcut($shortcutPath)
$sc.TargetPath = $exe
$sc.WorkingDirectory = $pub
$sc.Description = "XuanYi"
$sc.IconLocation = $iconPath
$sc.Save()
Write-Host ("Created: " + $shortcutPath)
