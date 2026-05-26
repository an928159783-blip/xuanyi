@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 (
  echo [错误] 未找到 .NET SDK。请先安装: https://dotnet.microsoft.com/download/dotnet/8.0
  exit /b 1
)
dotnet restore
if errorlevel 1 exit /b 1
dotnet build -c Release
if errorlevel 1 exit /b 1
echo.
echo 编译成功。运行: dotnet run --project HoverTranslate.App -c Release
exit /b 0
