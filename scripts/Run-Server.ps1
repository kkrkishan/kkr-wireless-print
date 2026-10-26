<#
.SYNOPSIS
    Starts the KKR Wireless Print Server WPF Desktop Dashboard.
#>

$publishExe = "$PSScriptRoot\..\publish\KKR.PrintServer.App.exe"
if (Test-Path $publishExe) {
    Write-Host "Launching KKR Wireless Print Server (Standalone)..." -ForegroundColor Green
    Start-Process $publishExe
    exit 0
}

$env:DOTNET_ROOT = "C:\Users\kisha\.dotnet"
$env:Path = "C:\Users\kisha\.dotnet;" + $env:Path
$env:DOTNET_CLI_HOME = "$PSScriptRoot\..\.dotnet_cli"
$env:NUGET_PACKAGES = "$PSScriptRoot\..\.nuget\packages"
$env:APPDATA = "$PSScriptRoot\..\.appdata"

$debugExe = "$PSScriptRoot\..\src\KKR.PrintServer.App\bin\Debug\net8.0-windows10.0.19041.0\KKR.PrintServer.App.exe"

if (-not (Test-Path $debugExe)) {
    Write-Host "Building KKR Wireless Print..." -ForegroundColor Cyan
    dotnet build "$PSScriptRoot\..\src\KKR.PrintServer.App\KKR.PrintServer.App.csproj" -c Debug
}

Write-Host "Launching KKR Wireless Print Server..." -ForegroundColor Green
Start-Process $debugExe
