@echo off
taskkill /IM HoverTranslate.exe /F >nul 2>&1
set "ROOT=%~dp0.."
cd /d "%ROOT%"
echo Publishing and updating desktop shortcut...
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\scripts\create-desktop-shortcut.ps1"
if errorlevel 1 (
  echo BUILD OR SHORTCUT FAILED.
  pause
  exit /b 1
)
set "EXE=%ROOT%\HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish\HoverTranslate.exe"
start "" "%EXE%"
echo Started. Desktop 炫译.lnk points to latest publish. Check tray.
pause
