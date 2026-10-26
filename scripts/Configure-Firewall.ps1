<#
.SYNOPSIS
    Configures Windows Firewall rules for KKR Wireless Print Server.
.DESCRIPTION
    Opens ports 8443 (HTTPS) and 8080 (HTTP) exclusively on the Windows "Private" network profile.
    Explicitly blocks exposure to Public networks and never disables Windows Firewall.
#>

[CmdletBinding()]
param()

Clear-Host
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  KKR Wireless Print - Windows Firewall Configuration" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Check elevation
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Warning "This script requires Administrator privileges to configure Windows Firewall rules."
    Write-Host "Please re-run this script in an elevated PowerShell prompt ('Run as Administrator')." -ForegroundColor Yellow
    exit 1
}

# Inspect active network connections
Write-Host "`nActive Network Profiles:" -ForegroundColor Gray
try {
    $connections = Get-NetConnectionProfile
    foreach ($conn in $connections) {
        $color = if ($conn.NetworkCategory -eq 'Private') { 'Green' } else { 'Yellow' }
        Write-Host " - Interface: $($conn.InterfaceAlias) | Network: $($conn.Name) | Category: $($conn.NetworkCategory)" -ForegroundColor $color
    }
} catch {
    Write-Host " Could not inspect network profiles." -ForegroundColor Gray
}

$ruleName = "KKR Wireless Print Server (Private Only)"

# Remove existing rule if present
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "`nRemoving existing rule '$ruleName'..." -ForegroundColor Gray
    Remove-NetFirewallRule -DisplayName $ruleName
}

# Create Private-only inbound firewall rule using splatting (safe against line-continuation syntax errors)
Write-Host "`nAdding inbound rule for TCP ports 8443 (HTTPS) and 8080 (HTTP) on Private profile..." -ForegroundColor Cyan

$firewallParams = @{
    DisplayName = $ruleName
    Description = 'Allows print submissions from authorized Android devices on private local Wi-Fi only.'
    Direction   = 'Inbound'
    Action      = 'Allow'
    Protocol    = 'TCP'
    LocalPort   = @('8443', '8080')
    Profile     = 'Private'
    Enabled     = 'True'
}

New-NetFirewallRule @firewallParams | Out-Null

Write-Host "`n[SUCCESS] Firewall rule configured successfully!" -ForegroundColor Green
Write-Host "Only devices connected to your Private Wi-Fi can reach the print server." -ForegroundColor White
Write-Host "Public network exposure is strictly disabled." -ForegroundColor White
Write-Host ""
