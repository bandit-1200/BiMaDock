@echo off
setlocal
cd /d "%~dp0"
if errorlevel 1 exit /b 1

dotnet publish "BiMaDock.csproj" --configuration Release --runtime win-x64 -p:PublishReadyToRun=true --output "publish"
if errorlevel 1 exit /b %errorlevel%

set "APPVERSION="
for /f "usebackq delims=" %%v in (`powershell -NoProfile -ExecutionPolicy Bypass -File "get_version.ps1"`) do set "APPVERSION=%%v"
if not defined APPVERSION (
    echo Version could not be read from version.json.
    exit /b 1
)

set "ISCC="
where ISCC.exe >nul 2>nul
if not errorlevel 1 set "ISCC=ISCC.exe"
if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC (
    echo Inno Setup 6 compiler ISCC.exe was not found.
    exit /b 1
)

"%ISCC%" "/DMyAppVersion=%APPVERSION%" "BiMaDock_local.iss"
if errorlevel 1 exit /b %errorlevel%

echo Installer created in the setup folder.
