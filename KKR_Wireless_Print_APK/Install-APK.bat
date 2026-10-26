@echo off
setlocal
echo ========================================================
echo       KKR Wireless Print - Android APK Installer
echo ========================================================
echo.

where adb >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] 'adb' command not found in PATH.
    echo.
    echo You can either:
    echo 1. Copy the APK file to your phone via USB cable or WhatsApp / Google Drive / Bluetooth
    echo    and tap it to install (Enable 'Install unknown apps' in Android Settings).
    echo 2. Or add Android platform-tools to your PATH and re-run this script.
    echo.
    pause
    exit /b 1
)

echo Checking for connected Android devices...
adb devices
echo.

if exist "%~dp0kkr-wireless-print-release.apk" (
    echo Installing Release APK: kkr-wireless-print-release.apk ...
    adb install -r -d "%~dp0kkr-wireless-print-release.apk"
) else (
    echo Installing Debug APK: kkr-wireless-print-debug.apk ...
    adb install -r -d "%~dp0kkr-wireless-print-debug.apk"
)

if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS] KKR Wireless Print has been successfully installed on your phone!
    echo Look for the 'KKR Wireless Print' app icon on your home screen or app drawer.
) else (
    echo.
    echo [NOTE] If installation failed, make sure:
    echo - USB Debugging is enabled on your phone (Developer Options).
    echo - You have accepted the 'Allow USB debugging' prompt on your phone's screen.
    echo - Alternatively, transfer the APK directly to your phone and install it manually.
)

echo.
pause
