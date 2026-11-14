; The installed files, folder, and identifiers keep LightBulb's names, so that installing Afterglow
; upgrades an existing LightBulb installation in place, keeping its settings and startup entry
#define AppName "LightBulb"
#define AppDisplayName "Afterglow"
#define AppUrl "https://github.com/trentrichards1/Afterglow"
#define AppVersion GetEnv("INSTALLER_APP_VERSION")

[Setup]
AppId={{892F745F-A497-42ED-B503-8D74936D0BEB}
AppName={#AppDisplayName}
AppVersion={#AppVersion}
AppVerName={#AppDisplayName} {#AppVersion}
AppPublisher="{#AppDisplayName}"
AppPublisherURL="{#AppUrl}"
AppSupportURL="{#AppUrl}/issues"
AppUpdatesURL="{#AppUrl}/releases"
AppMutex=LightBulb_Identity
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppDisplayName}
AllowNoIcons=yes
DisableWelcomePage=yes
DisableProgramGroupPage=no
DisableReadyPage=yes
SetupIconFile=..\favicon.ico
UninstallDisplayIcon={app}\LightBulb.exe
OutputDir=bin\
OutputBaseFilename=LightBulb-Installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: ".installed"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\License.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "Source\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppDisplayName}"; Filename: "{app}\LightBulb.exe"
Name: "{group}\{cm:UninstallProgram,{#AppDisplayName}}"; Filename: "{uninstallexe}"
Name: "{group}\{#AppDisplayName} on GitHub"; Filename: "{#AppUrl}"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows NT\CurrentVersion\ICM"; ValueType: dword; ValueName: "GdiICMGammaRange"; ValueData: "256"

[Run]
Filename: "{app}\LightBulb.exe"; Description: "{cm:LaunchProgram,{#StringChange(AppDisplayName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Name: "{userappdata}\LightBulb"; Type: filesandordirs
