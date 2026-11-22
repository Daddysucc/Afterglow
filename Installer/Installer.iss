; Shares its AppId with LightBulb (and with Afterglow from before the rename, which was installed
; under LightBulb's name), so that Windows sees Afterglow as a newer version of the same app.
; An existing installation in a folder named after LightBulb is uninstalled first (keeping its
; settings and startup entry), and Afterglow is installed in a folder named after itself.
#define AppName "Afterglow"
#define AppLegacyName "LightBulb"
#define AppId "892F745F-A497-42ED-B503-8D74936D0BEB"
#define AppUrl "https://github.com/Daddysucc/Afterglow"
; Lists the app's files (relative to the installation folder), written by the release workflow
#define AppFilesListName ".files"
#define AppVersion GetEnv("INSTALLER_APP_VERSION")
; The version without any label (e.g. 3.2.0), as Windows shows it in the file's properties
#define AppVersionNumber GetEnv("INSTALLER_APP_VERSION_NUMBER")
; Architecture of the packaged app: x64, arm64, or x86 (no restriction when not set)
#define AppArchitecture GetEnv("INSTALLER_APP_ARCHITECTURE")

[Setup]
; 64-bit apps go to "Program Files" (rather than "Program Files (x86)"), and each installer can
; only be run where its app can
#if AppArchitecture == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif AppArchitecture == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#endif
AppId={{{#AppId}}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher="{#AppName}"
AppPublisherURL="{#AppUrl}"
AppSupportURL="{#AppUrl}/issues"
AppUpdatesURL="{#AppUrl}/releases"
; The installer says it's Afterglow, in the version being released (code signing checks both,
; see the .signpath folder)
VersionInfoProductName={#AppName}
VersionInfoProductTextVersion={#AppVersion}
#if AppVersionNumber != ""
VersionInfoVersion={#AppVersionNumber}
VersionInfoProductVersion={#AppVersionNumber}
#endif
; Neither this app nor an older copy (or LightBulb) can be running while files are replaced
AppMutex={#AppName}_Identity,{#AppLegacyName}_Identity
; Where to install is decided in the code below, rather than always reusing the previous folder,
; so that an installation in a folder named after LightBulb moves to one named after Afterglow
DefaultDirName={code:GetDefaultDirName}
UsePreviousAppDir=no
DefaultGroupName={#AppName}
UsePreviousGroup=no
AllowNoIcons=yes
DisableWelcomePage=yes
DisableProgramGroupPage=no
DisableReadyPage=yes
SetupIconFile=..\favicon.ico
UninstallDisplayIcon={app}\{#AppName}.exe
OutputDir=bin\
OutputBaseFilename=Afterglow-Installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[InstallDelete]
; Files from before the rename, when upgrading in place (in a folder with a name of its own)
Type: files; Name: "{app}\{#AppLegacyName}*.*"

[Files]
Source: ".installed"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\License.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "Source\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppName}.exe"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{group}\{#AppName} on GitHub"; Filename: "{#AppUrl}"

[Registry]
; Windows only reads this from the 64-bit registry on 64-bit systems, even for the x86 build
Root: HKLM64; Subkey: "Software\Microsoft\Windows NT\CurrentVersion\ICM"; ValueType: dword; ValueName: "GdiICMGammaRange"; ValueData: "256"; Check: IsWin64
Root: HKLM; Subkey: "Software\Microsoft\Windows NT\CurrentVersion\ICM"; ValueType: dword; ValueName: "GdiICMGammaRange"; ValueData: "256"; Check: not IsWin64

[Run]
Filename: "{app}\{#AppName}.exe"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
; When the app updates itself (installing silently), it's started again afterwards, without
; administrator rights (as postinstall entries run as the user who started the installer)
Filename: "{app}\{#AppName}.exe"; Flags: nowait postinstall; Check: IsUpdating

[UninstallDelete]
Name: "{userappdata}\{#AppName}"; Type: filesandordirs
; Settings left from before the rename (they're copied over, not moved)
Name: "{userappdata}\{#AppLegacyName}"; Type: filesandordirs

[Code]
const
  UninstallKeyName = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{{#AppId}}_is1';
  RunKeyName = 'Software\Microsoft\Windows\CurrentVersion\Run';
  StartHiddenArgument = '--start-hidden';

var
  // The existing installation, if there is one
  PreviousDirPath: String;
  PreviousUninstallerPath: String;

  // Set once the installation has finished, so that a failed update can start the app again
  IsInstalled: Boolean;

  // The files of the version being replaced (relative to the installation folder), as listed by
  // the release workflow, so that those the new version no longer has can be removed
  PreviousAppFiles: TArrayOfString;

const
  Synchronize = $00100000;

function OpenProcess(DesiredAccess: DWORD; InheritHandle: BOOL; ProcessId: DWORD): THandle;
  external 'OpenProcess@kernel32.dll stdcall';
function WaitForSingleObject(Handle: THandle; Milliseconds: DWORD): DWORD;
  external 'WaitForSingleObject@kernel32.dll stdcall';
function CloseHandle(Handle: THandle): BOOL;
  external 'CloseHandle@kernel32.dll stdcall';

// Whether the app started this installer to update itself (see UpdateHandoff in the app)
function IsUpdating(): Boolean;
begin
  Result := ExpandConstant('{param:UPDATE|0}') = '1';
end;

// Waits for the app that started this update to close, so that its files can be replaced
procedure WaitForUpdatingAppToExit();
var
  ProcessId: Integer;
  Handle: THandle;
begin
  ProcessId := StrToIntDef(ExpandConstant('{param:UPDATEPID|0}'), 0);
  if ProcessId <= 0 then
    Exit;

  Handle := OpenProcess(Synchronize, False, ProcessId);
  if Handle = 0 then
    Exit;

  try
    WaitForSingleObject(Handle, 60000);
  finally
    CloseHandle(Handle);
  end;
end;

// Lets the app know that this installer is running (with administrator rights), so that it
// closes and lets itself be updated
procedure ReportUpdateReady();
var
  ReadyFilePath: String;
begin
  ReadyFilePath := ExpandConstant('{param:UPDATEREADYFILE|}');
  if ReadyFilePath <> '' then
    SaveStringToFile(ReadyFilePath, 'ready', False);
end;

function IsSameDir(const A, B: String): Boolean;
begin
  Result := CompareText(RemoveBackslashUnlessRoot(A), RemoveBackslashUnlessRoot(B)) = 0;
end;

function TryReadPreviousInstallation(const RootKey: Integer): Boolean;
var
  DirPath, UninstallCommand: String;
begin
  Result := False;

  if not RegQueryStringValue(RootKey, UninstallKeyName, 'Inno Setup: App Path', DirPath) then
    Exit;

  if not RegQueryStringValue(RootKey, UninstallKeyName, 'UninstallString', UninstallCommand) then
    Exit;

  if (DirPath = '') or (UninstallCommand = '') then
    Exit;

  PreviousDirPath := RemoveBackslashUnlessRoot(DirPath);
  PreviousUninstallerPath := RemoveQuotes(UninstallCommand);
  Result := True;
end;

// Finds an existing installation, which may have been installed as a 32-bit or 64-bit app (the
// original LightBulb was always installed as a 32-bit one), for everyone or just this user
procedure FindPreviousInstallation();
begin
  PreviousDirPath := '';
  PreviousUninstallerPath := '';

  if IsWin64 then
  begin
    if TryReadPreviousInstallation(HKLM64) then
      Exit;
    if TryReadPreviousInstallation(HKLM32) then
      Exit;
  end
  else if TryReadPreviousInstallation(HKLM) then
    Exit;

  TryReadPreviousInstallation(HKCU);
end;

function InitializeSetup(): Boolean;
begin
  if IsUpdating() then
  begin
    ReportUpdateReady();
    WaitForUpdatingAppToExit();
  end;

  FindPreviousInstallation();

  if PreviousDirPath <> '' then
    Log('Found an existing installation in: ' + PreviousDirPath);

  Result := True;
end;

function IsProgramFilesDir(const DirPath: String): Boolean;
begin
  Result :=
    IsSameDir(DirPath, ExpandConstant('{autopf}')) or
    IsSameDir(DirPath, ExpandConstant('{commonpf32}'));

  if IsWin64 then
    Result := Result or IsSameDir(DirPath, ExpandConstant('{commonpf64}'));
end;

// New installations go to Program Files. An existing installation in a folder named after
// LightBulb moves next to it (or to Program Files, if it was there), into a folder named after
// Afterglow. One in a folder with a name of its own is upgraded where it is.
function GetDefaultDirName(Param: String): String;
var
  ParentDirPath: String;
begin
  if PreviousDirPath = '' then
    Result := ExpandConstant('{autopf}\{#AppName}')
  else if CompareText(ExtractFileName(PreviousDirPath), '{#AppLegacyName}') = 0 then
  begin
    ParentDirPath := ExtractFileDir(PreviousDirPath);

    if IsProgramFilesDir(ParentDirPath) then
      Result := ExpandConstant('{autopf}\{#AppName}')
    else
      Result := AddBackslash(ParentDirPath) + '{#AppName}';
  end
  else
    Result := PreviousDirPath;
end;

// The previous version's uninstaller deletes its settings folder, so the settings are copied
// to Afterglow's own folder first (unless it already has settings, which are newer). Returns
// false if they couldn't be copied, in which case nothing must be uninstalled.
function CopyLegacySettings(): Boolean;
var
  SourcePath, TargetPath: String;
begin
  Result := True;

  SourcePath := ExpandConstant('{userappdata}\{#AppLegacyName}\Settings.json');
  TargetPath := ExpandConstant('{userappdata}\{#AppName}\Settings.json');

  if not FileExists(SourcePath) or FileExists(TargetPath) then
    Exit;

  Result := ForceDirectories(ExtractFileDir(TargetPath)) and FileCopy(SourcePath, TargetPath, True);

  if Result then
    Log('Copied the settings to: ' + TargetPath)
  else
    Log('Failed to copy the settings to: ' + TargetPath);
end;

// Runs before any files are installed
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';

  // Nothing to remove, or an upgrade in place (where the new files replace the old ones)
  if (PreviousDirPath = '') or IsSameDir(PreviousDirPath, ExpandConstant('{app}')) then
    Exit;

  if not CopyLegacySettings() then
  begin
    Result :=
      'Your settings from the previous version could not be copied, so it was left installed. ' +
      'Uninstall it from Settings > Apps, then run this installer again.';
    Exit;
  end;

  // Its folder may have been deleted by hand, leaving just the registry entry, which this
  // installation replaces anyway
  if not FileExists(PreviousUninstallerPath) then
  begin
    Log('The previous uninstaller is missing: ' + PreviousUninstallerPath);
    Exit;
  end;

  Log('Uninstalling the previous version: ' + PreviousUninstallerPath);

  if not Exec(
    PreviousUninstallerPath,
    '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode
  ) or (ResultCode <> 0) then
  begin
    Result :=
      'The previous version, installed in "' + PreviousDirPath + '", could not be removed. ' +
      'Uninstall it from Settings > Apps, then run this installer again. Your settings have been kept.';
  end;
end;

// The executable path at the start of a startup entry, e.g. "C:\Apps\LightBulb.exe" --start-hidden
function ExtractQuotedPath(const Command: String): String;
var
  Rest: String;
  ClosingQuoteIndex: Integer;
begin
  Result := '';

  if Copy(Command, 1, 1) <> '"' then
    Exit;

  Rest := Copy(Command, 2, Length(Command) - 1);
  ClosingQuoteIndex := Pos('"', Rest);

  if ClosingQuoteIndex > 1 then
    Result := Copy(Rest, 1, ClosingQuoteIndex - 1);
end;

// Moves the previous version's startup entry over to Afterglow, so it keeps starting with
// Windows even if it isn't launched before the next restart. Only an entry that launched the
// previous version (now removed or replaced) is taken over: one that launches a different copy
// that still exists elsewhere (e.g. a portable LightBulb) is left alone.
procedure MoveLegacyStartupEntry();
var
  Command, ExecutablePath: String;
begin
  if not RegQueryStringValue(HKCU, RunKeyName, '{#AppLegacyName}', Command) then
    Exit;

  ExecutablePath := ExtractQuotedPath(Command);
  if ExecutablePath = '' then
    Exit;

  if FileExists(ExecutablePath) and not IsSameDir(ExtractFileDir(ExecutablePath), ExpandConstant('{app}')) then
    Exit;

  if not RegValueExists(HKCU, RunKeyName, '{#AppName}') then
    RegWriteStringValue(
      HKCU,
      RunKeyName,
      '{#AppName}',
      '"' + ExpandConstant('{app}\{#AppName}.exe') + '" ' + StartHiddenArgument
    );

  RegDeleteValue(HKCU, RunKeyName, '{#AppLegacyName}');
  Log('Moved the startup entry over from: ' + ExecutablePath);
end;

// ---- Files that a new version no longer has ----

// The list of the app's files, written next to them by the release workflow
function GetAppFilesListPath(): String;
begin
  Result := ExpandConstant('{app}\{#AppFilesListName}');
end;

// A path from the list, as a path relative to the installation folder, or '' if it isn't one
// (so that nothing outside the installation folder can ever be removed)
function NormalizeListedPath(const Path: String): String;
begin
  Result := Trim(Path);
  StringChangeEx(Result, '/', '\', True);

  if (Result = '') or (Pos('..', Result) > 0) or (Pos(':', Result) > 0) or
    (Copy(Result, 1, 1) = '\') or (Pos('*', Result) > 0) or (Pos('?', Result) > 0) then
    Result := '';
end;

function IsListed(const Paths: TArrayOfString; const Path: String): Boolean;
var
  I: Integer;
begin
  Result := False;

  for I := 0 to GetArrayLength(Paths) - 1 do
  begin
    if CompareText(NormalizeListedPath(Paths[I]), Path) = 0 then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

// Remembers the files of the version being replaced, before they're overwritten
procedure LoadPreviousAppFiles();
begin
  SetArrayLength(PreviousAppFiles, 0);

  if FileExists(GetAppFilesListPath()) and not LoadStringsFromFile(GetAppFilesListPath(), PreviousAppFiles) then
    SetArrayLength(PreviousAppFiles, 0);
end;

// Removes the folders that held a removed file, as long as they're empty
procedure RemoveEmptyDirs(DirPath: String);
var
  AppDirPath: String;
begin
  AppDirPath := ExpandConstant('{app}');

  while (Length(DirPath) > Length(AppDirPath)) and not IsSameDir(DirPath, AppDirPath) do
  begin
    if not RemoveDir(DirPath) then
      Exit;

    DirPath := ExtractFileDir(DirPath);
  end;
end;

// Once the new version is in place, removes the files of the previous version that it no longer
// has. Only files that the previous version listed as its own are ever removed, and only if the
// new version lists its own files too.
procedure RemoveObsoleteAppFiles();
var
  CurrentAppFiles: TArrayOfString;
  I: Integer;
  RelativePath, FilePath: String;
begin
  if GetArrayLength(PreviousAppFiles) = 0 then
    Exit;

  if not LoadStringsFromFile(GetAppFilesListPath(), CurrentAppFiles) or (GetArrayLength(CurrentAppFiles) = 0) then
    Exit;

  for I := 0 to GetArrayLength(PreviousAppFiles) - 1 do
  begin
    RelativePath := NormalizeListedPath(PreviousAppFiles[I]);
    if (RelativePath = '') or IsListed(CurrentAppFiles, RelativePath) then
      Continue;

    FilePath := ExpandConstant('{app}\') + RelativePath;
    if FileExists(FilePath) and DeleteFile(FilePath) then
    begin
      Log('Removed a file the new version no longer has: ' + RelativePath);
      RemoveEmptyDirs(ExtractFileDir(FilePath));
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  // Right before the new files are copied
  if CurStep = ssInstall then
    LoadPreviousAppFiles();

  if CurStep = ssPostInstall then
  begin
    IsInstalled := True;
    RemoveObsoleteAppFiles();
    MoveLegacyStartupEntry();
  end;
end;

// If an update didn't install (e.g. it failed, or was cancelled), the app closed for nothing,
// so the version that's still installed is started again
procedure DeinitializeSetup();
var
  ExecutablePath: String;
  ResultCode: Integer;
begin
  if not IsUpdating() or IsInstalled then
    Exit;

  try
    ExecutablePath := ExpandConstant('{app}\{#AppName}.exe');
    if FileExists(ExecutablePath) then
      ExecAsOriginalUser(ExecutablePath, '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
  except
    // Setup ended before the installation folder was known
    Log('Could not start the app again: ' + GetExceptionMessage());
  end;
end;
