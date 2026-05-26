@echo off
taskkill /IM HoverTranslate.exe /F >nul 2>&1
set "ROOT=%~dp0.."
cd /d "%ROOT%"
echo Publishing latest build...
dotnet publish HoverTranslate.App\HoverTranslate.App.csproj -c Release -r win-x64 --self-contained false
if errorlevel 1 (
  echo BUILD FAILED.
  pause
  exit /b 1
)
set "EXE=%ROOT%\HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish\HoverTranslate.exe"
cd /d "%ROOT%\HoverTranslate.App\bin\Release\net8.0-windows\win-x64\publish"
start "" "%EXE%"
echo Started from publish folder. Check tray.
pause
