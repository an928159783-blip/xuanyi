@echo off
set "ROOT=%~dp0.."
cd /d "%ROOT%"
echo Publishing and updating desktop shortcut...
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\scripts\create-desktop-shortcut.ps1"
if errorlevel 1 (
  echo BUILD OR SHORTCUT FAILED.
  pause
  exit /b 1
)
for /f "delims=" %%P in ('powershell -NoProfile -Command "$d=Get-ChildItem '%ROOT%\HoverTranslate.App\bin\Release' -Recurse -Filter publish -ErrorAction SilentlyContinue ^| Where-Object { Test-Path (Join-Path $_.FullName 'HoverTranslate.exe') } ^| Sort-Object { (Get-Item (Join-Path $_.FullName 'HoverTranslate.exe')).LastWriteTime } -Descending ^| Select-Object -First 1 -ExpandProperty FullName; Write-Output $d"') do set "PUB=%%P"
set "EXE=%PUB%\HoverTranslate.exe"
start "" "%EXE%"
echo Started. Desktop shortcut points to: %EXE%
pause
