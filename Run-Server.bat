@echo off
title KKR Wireless Print Server
cd /d "%~dp0"

echo ==========================================================
echo   Starting KKR Wireless Print Server...
echo ==========================================================

REM 1. Stop any old or background instances
taskkill /F /IM KKR.PrintServer.App.exe >NUL 2>&1
timeout /t 1 /nobreak >NUL

REM 2. Check KKR_Print_Server folder
if exist "%~dp0KKR_Print_Server\KKR.PrintServer.App.exe" (
    cd /d "%~dp0KKR_Print_Server"
    start "" "KKR.PrintServer.App.exe"
    exit /b 0
)

REM 2. Check publish folder
if exist "%~dp0publish\KKR.PrintServer.App.exe" (
    cd /d "%~dp0publish"
    start "" "KKR.PrintServer.App.exe"
    exit /b 0
)

REM 3. Fallback to debug build
set "DOTNET_ROOT=C:\Users\kisha\.dotnet"
set "Path=C:\Users\kisha\.dotnet;%Path%"
if exist "%~dp0src\KKR.PrintServer.App\bin\Debug\net8.0-windows10.0.19041.0\KKR.PrintServer.App.exe" (
    cd /d "%~dp0src\KKR.PrintServer.App\bin\Debug\net8.0-windows10.0.19041.0"
    start "" "KKR.PrintServer.App.exe"
    exit /b 0
)

echo ERROR: Could not find KKR.PrintServer.App.exe
pause
