; Inno Setup 6 Script for KKR Wireless Print
; Professional, self-contained Windows Installer

#define MyAppName "KKR Wireless Print"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "KKR Education"
#define MyAppURL "https://github.com/kkr-education/wireless-print"
#define MyAppExeName "KKR.PrintServer.App.exe"
#define MyAppId "{{9C5E487B-57A1-4F43-8531-18F67A8CB3B1}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=KKR_Wireless_Print_Setup
SetupIconFile=app.ico
WizardImageFile=wizard_large.bmp
WizardSmallImageFile=wizard_small.bmp
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog commandline
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
MinVersion=10.0.17763
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} Server
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "firewallrule"; Description: "Configure Windows Firewall for local Wi-Fi & LAN mobile access (Ports 8080 & 8443)"; GroupDescription: "Network Configuration:"; Flags: checkedonce

[Files]
Source: "..\build\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Fix-Firewall.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; WorkingDir: "{app}"
Name: "{autoprograms}\{#MyAppName}\Network & Firewall Helper"; Filename: "{app}\Fix-Firewall.bat"; WorkingDir: "{app}"
Name: "{autoprograms}\{#MyAppName}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
; Add Windows Firewall Port 8080 (HTTP) Rule
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""KKR Wireless Print Server (HTTP 8080)"" dir=in action=allow protocol=TCP localport=8080 profile=any program=""{app}\{#MyAppExeName}"""; Flags: runhidden; StatusMsg: "Configuring Windows Firewall for HTTP (port 8080)..."; Tasks: firewallrule

; Add Windows Firewall Port 8443 (HTTPS) Rule
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""KKR Wireless Print Server (HTTPS 8443)"" dir=in action=allow protocol=TCP localport=8443 profile=any program=""{app}\{#MyAppExeName}"""; Flags: runhidden; StatusMsg: "Configuring Windows Firewall for HTTPS (port 8443)..."; Tasks: firewallrule

; Add General Application Authorization Rule
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""KKR Wireless Print Application"" dir=in action=allow program=""{app}\{#MyAppExeName}"" enable=yes profile=any"; Flags: runhidden; StatusMsg: "Authorizing KKR Wireless Print application..."; Tasks: firewallrule

; Launch App option
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Remove Windows Firewall Rules
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""KKR Wireless Print Server (HTTP 8080)"""; Flags: runhidden; RunOnceId: "DelFwHttp"
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""KKR Wireless Print Server (HTTPS 8443)"""; Flags: runhidden; RunOnceId: "DelFwHttps"
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""KKR Wireless Print Application"""; Flags: runhidden; RunOnceId: "DelFwApp"

[Code]
// Terminate running application instances before installation or uninstallation
procedure StopServerProcess();
var
  ResultCode: Integer;
begin
  try
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  except
  end;
  Sleep(300);
end;

function InitializeSetup(): Boolean;
begin
  StopServerProcess();
  Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  StopServerProcess();
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\KKR_Wireless_Print');
    if DirExists(DataDir) then
    begin
      if MsgBox('Do you want to delete all saved print server logs, certificates, and runtime data in:' + #13#10 + DataDir + '?', mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
