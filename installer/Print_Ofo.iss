; Inno Setup 6 Script for Print Ofo
; Professional, self-contained Windows Installer for Microsoft Store & Direct Distribution

#define MyAppName "Print Ofo"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Print Ofo"
#define MyAppURL "https://github.com/kkrkishan/kkr-wireless-print"
#define MyAppExeName "PrintOfo.Server.exe"
#define MyAppId "{{E7918235-9C08-4B28-8FD5-3D794F817B62}}"

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
OutputBaseFilename=Print_Ofo_Setup
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
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""Print Ofo Server (HTTP 8080)"" dir=in action=allow protocol=TCP localport=8080 profile=any program=""{app}\{#MyAppExeName}"""; Flags: runhidden; StatusMsg: "Configuring Windows Firewall for HTTP (port 8080)..."; Tasks: firewallrule

; Add Windows Firewall Port 8443 (HTTPS) Rule
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""Print Ofo Server (HTTPS 8443)"" dir=in action=allow protocol=TCP localport=8443 profile=any program=""{app}\{#MyAppExeName}"""; Flags: runhidden; StatusMsg: "Configuring Windows Firewall for HTTPS (port 8443)..."; Tasks: firewallrule

; Add Windows Firewall Port 8089 (UDP Discovery) Rule
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""Print Ofo Server (UDP 8089)"" dir=in action=allow protocol=UDP localport=8089 profile=any program=""{app}\{#MyAppExeName}"""; Flags: runhidden; StatusMsg: "Configuring Windows Firewall for UDP Discovery (port 8089)..."; Tasks: firewallrule
; Add General Application Authorization Rule
Filename: "netsh.exe"; Parameters: "advfirewall firewall add rule name=""Print Ofo Server Application"" dir=in action=allow program=""{app}\{#MyAppExeName}"" enable=yes profile=any"; Flags: runhidden; StatusMsg: "Authorizing Print Ofo Server application..."; Tasks: firewallrule

; Launch App option
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Remove Windows Firewall Rules
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Print Ofo Server (HTTP 8080)"""; Flags: runhidden; RunOnceId: "DelFwHttp"
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Print Ofo Server (HTTPS 8443)"""; Flags: runhidden; RunOnceId: "DelFwHttps"
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Print Ofo Server (UDP 8089)"""; Flags: runhidden; RunOnceId: "DelFwUdp"
Filename: "netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Print Ofo Server Application"""; Flags: runhidden; RunOnceId: "DelFwApp"

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
    DataDir := ExpandConstant('{localappdata}\Print_Ofo');
    if DirExists(DataDir) then
    begin
      if MsgBox('Do you want to delete all saved Print Ofo logs, certificates, and runtime data in:' + #13#10 + DataDir + '?', mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
