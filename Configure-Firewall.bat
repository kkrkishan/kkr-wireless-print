@echo off
cd /d "%~dp0"
echo Requesting administrator privileges to configure firewall...
powershell -ExecutionPolicy Bypass -File "%~dp0scripts\Configure-Firewall.ps1"
pause
