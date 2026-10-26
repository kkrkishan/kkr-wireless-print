@echo off
setlocal
:: Check for Administrator privileges; if not elevated, prompt for UAC elevation
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ==========================================================
    echo   Requesting Administrator privileges to configure Firewall...
    echo ==========================================================
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process cmd -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)

echo ==========================================================
echo   KKR Wireless Print - Firewall ^& Network Auto-Fix
echo ==========================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference = 'SilentlyContinue'; " ^
    "Write-Host '[1/3] Setting Wi-Fi network profile to Private...' -ForegroundColor Cyan; " ^
    "$wifiProfiles = Get-NetConnectionProfile | Where-Object { $_.InterfaceAlias -like '*Wi-Fi*' }; " ^
    "foreach ($p in $wifiProfiles) { Set-NetConnectionProfile -Name $p.Name -NetworkCategory Private; Write-Host ('  Network ' + $p.Name + ' is now Private.') -ForegroundColor Green; }; " ^
    "Write-Host '[2/3] Opening TCP ports 8080 and 8443 on all firewall profiles...' -ForegroundColor Cyan; " ^
    "Remove-NetFirewallRule -DisplayName 'KKR Wireless Print*' -ErrorAction SilentlyContinue; " ^
    "New-NetFirewallRule -DisplayName 'KKR Wireless Print Server (Ports 8080, 8443)' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 8080,8443 -Profile Any -Enabled True | Out-Null; " ^
    "Write-Host '  Ports 8080 and 8443 are now open for all profiles.' -ForegroundColor Green; " ^
    "Write-Host '[3/3] Authorizing KKR.PrintServer.App.exe...' -ForegroundColor Cyan; " ^
    "New-NetFirewallRule -DisplayName 'KKR Wireless Print Server App' -Direction Inbound -Action Allow -Program 'C:\Google Antigravity\KKR WIRELESS PRINT\KKR_Print_Server\KKR.PrintServer.App.exe' -Profile Any -Enabled True | Out-Null; " ^
    "Write-Host '  Application rule added.' -ForegroundColor Green; " ^
    "Write-Host ''; " ^
    "Write-Host '==========================================================' -ForegroundColor Green; " ^
    "Write-Host ' [SUCCESS] Firewall ^& Wi-Fi configured successfully!' -ForegroundColor Green; " ^
    "Write-Host '==========================================================' -ForegroundColor Green; " ^
    "Write-Host ''; " ^
    "$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -like '*Wi-Fi*' }).IPAddress; " ^
    "Write-Host (' Laptop Wi-Fi IP Address : ' + $ip) -ForegroundColor Yellow; " ^
    "Write-Host ' Laptop Port             : 8080' -ForegroundColor Yellow; " ^
    "Write-Host ''; " ^
    "Write-Host 'Your mobile phone can now connect to this laptop!' -ForegroundColor White; "

echo.
pause
