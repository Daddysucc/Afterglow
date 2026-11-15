; The installed files, folder, and identifiers keep LightBulb's names, so that installing Afterglow
; upgrades an existing LightBulb installation in place, keeping its settings and startup entry
#define AppName "LightBulb"
#define AppDisplayName "Afterglow"
#define AppUrl "https://github.com/Daddysucc/Afterglow"
#define AppVersion GetEnv("INSTALLER_APP_VERSION")
; Architecture of the packaged app: x64, arm64, or x86 (no restriction when not set)
#define AppArchitecture GetEnv("INSTALLER_APP_ARCHITECTURE")

[Setup]
; 64-bit apps go to "Program Files" (rather than "Program Files (x86)"), and each installer can
; only be run where its app can. Existing installations are upgraded in their current folder.
#if AppArchitecture == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif AppArchitecture == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#endif
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
; Windows only reads this from the 64-bit registry on 64-bit systems, even for the x86 build
Root: HKLM64; Subkey: "Software\Microsoft\Windows NT\CurrentVersion\ICM"; ValueType: dword; ValueName: "GdiICMGammaRange"; ValueData: "256"; Check: IsWin64
Root: HKLM; Subkey: "Software\Microsoft\Windows NT\CurrentVersion\ICM"; ValueType: dword; ValueName: "GdiICMGammaRange"; ValueData: "256"; Check: not IsWin64

[Run]
Filename: "{app}\LightBulb.exe"; Description: "{cm:LaunchProgram,{#StringChange(AppDisplayName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Name: "{userappdata}\LightBulb"; Type: filesandordirs
