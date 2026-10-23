; Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
;
; Inno Setup wrapper of the office-PC installation: one Klacks-Setup-win-x64.exe instead of a ZIP with Installieren.cmd.
; It only asks for the country, runs scripts\Install-KlacksPackage.ps1 hidden and shows its progress; all installation
; logic stays in the PowerShell scripts (proven by the native Windows gate). Uninstall runs Uninstall-Klacks.ps1 hidden
; and keeps the database unless the user agrees (or /REMOVEDATA is given). If that script fails or cannot be started,
; the uninstallation is aborted before Inno removes anything, so Klacks stays listed under installed apps and can be
; uninstalled again; the script's report is kept as {app}\Deinstallationsbericht-*.txt.
;
; Defines (iscc /D...): Version = x.y.z[-suffix], VersionCore = x.y.z (defaults to Version), SourceDir = package staging folder (as built by New-KlacksOfficePackage.ps1),
; OutputDir = target folder, Sign = sign setup and uninstaller (needs /Ssigntool=<command>), TestStub = dev build that needs no administrator rights (never shipped).
;
; Silent use:  Klacks-Setup-win-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /REGION=CH /PORT=5180 /LOG=setup.log
;              unins000.exe /VERYSILENT [/REMOVEDATA]
; Exit code 0 = Klacks installed (it may still be starting), 1 = installation failed (report path in the log).
; Uninstaller exit code 0 = Klacks removed, non-zero = not removed (Klacks stays registered; see the uninstall report).

#ifndef Version
  #error Pass /DVersion=x.y.z
#endif
#ifndef VersionCore
  #define VersionCore Version
#endif
#ifndef SourceDir
  #error Pass /DSourceDir=<package staging folder>
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

#define DefaultPort "5180"
#define HealthWaitSeconds "600"
#define ReadyMarker "READY"
#define CompletedMarker "COMPLETED"
#define FailedMarker "FAILED"
#define ReportTimestampFormat "yyyymmdd-hhnnss"
#define UninstallReportPrefix "Deinstallationsbericht-"
#ifdef TestStub
  #define PowerShellExe "{sysnative}\WindowsPowerShell\v1.0\powershell.exe"
#else
  #define PowerShellExe "{sys}\WindowsPowerShell\v1.0\powershell.exe"
#endif
#define RegionsInclude SourcePath + "Regions.generated.inc"

[Setup]
AppId={{6F1B7D52-3C0E-4B8A-9E41-5A2D8C7F0B13}
AppName=Klacks
AppVersion={#Version}
AppPublisher=Heribert Gasparoli
VersionInfoVersion={#VersionCore}
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
AllowNoIcons=yes
#ifdef TestStub
PrivilegesRequired=lowest
DefaultDirName={autopf}\Klacks Setup
#else
PrivilegesRequired=admin
DefaultDirName={commonpf64}\Klacks Setup
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
MinVersion=10.0
#ifdef Sign
SignTool=signtool
SignedUninstaller=yes
#endif
OutputDir={#OutputDir}
OutputBaseFilename=Klacks-Setup-win-x64
Compression=lzma2/fast
SolidCompression=no
WizardStyle=modern
UninstallDisplayName=Klacks
SetupLogging=yes
CloseApplications=no
RestartApplications=no
LanguageDetectionMethod=uilanguage

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[CustomMessages]
english.RegionTitle=Country
english.RegionDescription=Which country is Klacks used in?
english.RegionPrompt=Klacks applies the holidays, time zone and working-time rules of this country:
english.NoRegion=No country profile
english.InstallingStep=Setting up Klacks. The first start can take a few minutes...
english.OpenKlacks=Open Klacks in the browser
english.OpenReport=Open the installation report
english.FinishReady=Klacks is running. The first login is admin@test.com / P@ssw0rt1; Klacks then asks you to create your own administrator account.
english.FinishStarting=Klacks is installed and still starting (the first start downloads about 700 MB of AI models). Open Klacks from the Start menu in a few minutes. The first login is admin@test.com / P@ssw0rt1.
english.FinishFailed=The installation did not finish. Please send the installation report to your contact person:
english.AskRemoveData=Also delete the database, settings and backups of Klacks?%n%nChoose No to keep your data for a later installation.
english.NewerInstalled=Klacks %1 is already installed (it updated itself). This setup contains the older version %2 and will not replace it.
english.InvalidParameter=Invalid value for /%1.
english.UninstallFailed=Klacks could not be removed completely. It stays in the list of installed apps so that you can start the uninstallation again from there.%n%nPlease restart the computer and then start the uninstallation again.
english.UninstallFailedReport=If it fails again, please send the uninstallation report to your contact person:
english.UninstallFailedContact=If it fails again, please contact your contact person.
german.RegionTitle=Land
german.RegionDescription=In welchem Land wird Klacks eingesetzt?
german.RegionPrompt=Klacks übernimmt die Feiertage, die Zeitzone und die Arbeitszeitregeln dieses Landes:
german.NoRegion=Kein Länderprofil
german.InstallingStep=Klacks wird eingerichtet. Der erste Start kann einige Minuten dauern...
german.OpenKlacks=Klacks im Browser öffnen
german.OpenReport=Installationsbericht öffnen
german.FinishReady=Klacks läuft. Erste Anmeldung: admin@test.com / P@ssw0rt1; Klacks verlangt danach ein eigenes Administrator-Konto.
german.FinishStarting=Klacks ist installiert und startet noch (der erste Start lädt ca. 700 MB KI-Modelle). Öffnen Sie Klacks in einigen Minuten über das Startmenü. Erste Anmeldung: admin@test.com / P@ssw0rt1.
german.FinishFailed=Die Installation wurde nicht abgeschlossen. Bitte senden Sie den Installationsbericht an Ihre Ansprechperson:
german.AskRemoveData=Sollen auch die Datenbank, die Einstellungen und die Sicherungen von Klacks gelöscht werden?%n%nMit Nein bleiben Ihre Daten für eine spätere Installation erhalten.
german.NewerInstalled=Klacks %1 ist bereits installiert (es hat sich selbst aktualisiert). Dieses Setup enthält die ältere Version %2 und ersetzt sie nicht.
german.InvalidParameter=Ungültiger Wert für /%1.
german.UninstallFailed=Klacks konnte nicht vollständig entfernt werden. Klacks bleibt in der Liste der installierten Apps, damit Sie die Deinstallation dort erneut starten können.%n%nBitte starten Sie den Computer neu und starten Sie die Deinstallation danach noch einmal.
german.UninstallFailedReport=Falls es wieder nicht klappt, senden Sie bitte den Deinstallationsbericht an Ihre Ansprechperson:
german.UninstallFailedContact=Falls es wieder nicht klappt, wenden Sie sich bitte an Ihre Ansprechperson.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{tmp}\package"; Flags: ignoreversion recursesubdirs createallsubdirs deleteafterinstall
Source: "{#SourceDir}\scripts\Uninstall-Klacks.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "{#SourceDir}\scripts\Get-KlacksDiagnostics.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion

[INI]
Filename: "{app}\Klacks.url"; Section: "InternetShortcut"; Key: "URL"; String: "http://localhost:{code:GetPort}"

[Icons]
Name: "{autoprograms}\Klacks"; Filename: "{app}\Klacks.url"
Name: "{autodesktop}\Klacks"; Filename: "{app}\Klacks.url"; Tasks: desktopicon

[UninstallDelete]
Type: files; Name: "{app}\Installationsbericht-*.txt"
Type: files; Name: "{app}\{#UninstallReportPrefix}*.txt"

[Run]
Filename: "http://localhost:{code:GetPort}"; Description: "{cm:OpenKlacks}"; Flags: postinstall shellexec nowait skipifsilent; Check: InstallSucceeded
Filename: "{code:GetReportFile}"; Description: "{cm:OpenReport}"; Flags: postinstall shellexec nowait skipifsilent; Check: InstallFailed

[Code]
const
  PollMilliseconds = 500;
  LOCALE_USER_DEFAULT = $0400;
  LOCALE_SISO3166CTRYNAME = $5A;
  MinPort = 1024;
  MaxPort = 65535;
  ExitCodeInstallFailed = 1;

function SetTimer(Window: LongWord; EventId: LongWord; Elapse: LongWord; TimerProc: LongWord): LongWord;
  external 'SetTimer@user32.dll stdcall';
function KillTimer(Window: LongWord; EventId: LongWord): Boolean;
  external 'KillTimer@user32.dll stdcall';
function GetLocaleInfoW(Locale: DWORD; LCType: DWORD; Data: String; Size: Integer): Integer;
  external 'GetLocaleInfoW@kernel32.dll stdcall';

var
  RegionPage: TWizardPage;
  RegionCombo: TNewComboBox;
  RegionCodes: TStringList;
  Failed: Boolean;
  Ready: Boolean;
  ReportFile: String;
  InstallStatusFile: String;

#if FileExists(RegionsInclude)
  #include RegionsInclude
#else
procedure FillRegions(Names, Codes: TStrings);
begin
end;
#endif

function ParamPresent(const Name: String): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), '/' + Name) = 0 then
      Result := True;
end;

function IsDigits(const Value: String): Boolean;
var
  I: Integer;
begin
  Result := Length(Value) > 0;
  for I := 1 to Length(Value) do
    if (Value[I] < '0') or (Value[I] > '9') then
      Result := False;
end;

function GetPort(Param: String): String;
var
  Value: Integer;
begin
  Result := ExpandConstant('{param:PORT|{#DefaultPort}}');
  Value := StrToIntDef(Result, 0);
  if (not IsDigits(Result)) or (Value < MinPort) or (Value > MaxPort) then
  begin
    SuppressibleMsgBox(FmtMessage(CustomMessage('InvalidParameter'), ['PORT']), mbError, MB_OK, IDOK);
    Result := '{#DefaultPort}';
  end;
end;

function GetSystemCountry: String;
var
  Buffer: String;
  Size: Integer;
begin
  SetLength(Buffer, 16);
  Size := GetLocaleInfoW(LOCALE_USER_DEFAULT, LOCALE_SISO3166CTRYNAME, Buffer, 16);
  if Size > 1 then
    Result := Uppercase(Copy(Buffer, 1, Size - 1))
  else
    Result := '';
end;

function IsCountryCode(const Value: String): Boolean;
begin
  Result := (Length(Value) = 2) and (Uppercase(Value) >= 'AA') and (Uppercase(Value) <= 'ZZ');
end;

function ParseVersionPart(var Text: String): Integer;
var
  P: Integer;
begin
  P := Pos('.', Text);
  if P = 0 then
  begin
    Result := StrToIntDef(Text, 0);
    Text := '';
  end
  else
  begin
    Result := StrToIntDef(Copy(Text, 1, P - 1), 0);
    Text := Copy(Text, P + 1, Length(Text));
  end;
end;

function CompareVersions(A, B: String): Integer;
var
  I, PartA, PartB: Integer;
begin
  Result := 0;
  for I := 1 to 3 do
  begin
    PartA := ParseVersionPart(A);
    PartB := ParseVersionPart(B);
    if PartA <> PartB then
    begin
      if PartA > PartB then
        Result := 1
      else
        Result := -1;
      Exit;
    end;
  end;
end;

function HighestInstalledVersion: String;
var
  Find: TFindRec;
  Root: String;
begin
  Result := '';
  Root := ExpandConstant('{commonpf64}\Klacks\versions\');
  if FindFirst(Root + '*', Find) then
  try
    repeat
      if ((Find.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0) and (Find.Name <> '.') and (Find.Name <> '..') then
        if (Result = '') or (CompareVersions(Find.Name, Result) > 0) then
          Result := Find.Name;
    until not FindNext(Find);
  finally
    FindClose(Find);
  end;
end;

function InitializeSetup: Boolean;
var
  Installed: String;
begin
  Result := True;
  Installed := HighestInstalledVersion;
  if (Installed <> '') and (CompareVersions(Installed, '{#VersionCore}') > 0) then
  begin
    SuppressibleMsgBox(FmtMessage(CustomMessage('NewerInstalled'), [Installed, '{#Version}']), mbError, MB_OK, IDOK);
    Result := False;
  end;
end;

procedure InitializeWizard;
var
  Prompt: TNewStaticText;
  Names: TStringList;
  I: Integer;
begin
  RegionCodes := TStringList.Create;
  RegionPage := CreateCustomPage(wpWelcome, CustomMessage('RegionTitle'), CustomMessage('RegionDescription'));

  Prompt := TNewStaticText.Create(RegionPage);
  Prompt.Parent := RegionPage.Surface;
  Prompt.Caption := CustomMessage('RegionPrompt');
  Prompt.AutoSize := False;
  Prompt.WordWrap := True;
  Prompt.Width := RegionPage.SurfaceWidth;
  Prompt.Height := ScaleY(32);

  RegionCombo := TNewComboBox.Create(RegionPage);
  RegionCombo.Parent := RegionPage.Surface;
  RegionCombo.Style := csDropDownList;
  RegionCombo.Top := Prompt.Top + Prompt.Height + ScaleY(8);
  RegionCombo.Width := RegionPage.SurfaceWidth;

  Names := TStringList.Create;
  try
    FillRegions(Names, RegionCodes);
    RegionCombo.Items.Add(CustomMessage('NoRegion'));
    RegionCodes.Insert(0, '');
    for I := 0 to Names.Count - 1 do
      RegionCombo.Items.Add(Names[I]);
  finally
    Names.Free;
  end;

  RegionCombo.ItemIndex := RegionCodes.IndexOf(GetSystemCountry);
  if RegionCombo.ItemIndex < 0 then
    RegionCombo.ItemIndex := 0;
end;

function GetRegion: String;
begin
  Result := Uppercase(ExpandConstant('{param:REGION|}'));
  if Result <> '' then
  begin
    if not IsCountryCode(Result) then
    begin
      SuppressibleMsgBox(FmtMessage(CustomMessage('InvalidParameter'), ['REGION']), mbError, MB_OK, IDOK);
      Result := '';
    end;
  end
  else if (RegionCombo <> nil) and (RegionCombo.ItemIndex >= 0) and (RegionCombo.ItemIndex < RegionCodes.Count) then
    Result := RegionCodes[RegionCombo.ItemIndex];
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (RegionPage <> nil) and (PageID = RegionPage.ID) and (ExpandConstant('{param:REGION|}') <> '');
end;

function GetReportFile(Param: String): String;
begin
  Result := ReportFile;
end;

function InstallSucceeded: Boolean;
begin
  Result := not Failed;
end;

function InstallFailed: Boolean;
begin
  Result := Failed;
end;

function ReadLastStatusLine(const StatusFile: String): String;
var
  Lines: TArrayOfString;
begin
  Result := '';
  try
    if LoadStringsFromFile(StatusFile, Lines) and (GetArrayLength(Lines) > 0) then
      Result := Lines[GetArrayLength(Lines) - 1];
  except
  end;
end;

procedure CopyReportForUser;
var
  Target: String;
begin
  if not FileExists(ReportFile) then
    Exit;
  Target := ExpandConstant('{app}\') + ExtractFileName(ReportFile);
  if FileCopy(ReportFile, Target, False) then
    ReportFile := Target
  else
    Log('Could not copy the installation report to ' + Target);
end;

procedure PollStatus(Window, Message, EventId, Time: LongWord);
var
  Line: String;
begin
  Line := ReadLastStatusLine(InstallStatusFile);
  if (Line <> '') and (Line <> '{#ReadyMarker}') and (Line <> '{#CompletedMarker}') and (Line <> '{#FailedMarker}') then
    WizardForm.FilenameLabel.Caption := Line;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Arguments, Region, LastLine: String;
  ResultCode: Integer;
  TimerId: LongWord;
begin
  if CurStep <> ssPostInstall then
    Exit;

  InstallStatusFile := ExpandConstant('{tmp}\klacks-install-status.txt');
  DeleteFile(InstallStatusFile);
  ReportFile := ExpandConstant('{commonappdata}\Klacks\logs\Installationsbericht-') +
    GetDateTimeString('{#ReportTimestampFormat}', #0, #0) + '.txt';
  Region := GetRegion;

  Arguments := '-NoProfile -ExecutionPolicy Bypass -File "' +
    ExpandConstant('{tmp}\package\scripts\Install-KlacksPackage.ps1') + '"' +
    ' -Unattended -HttpPort ' + GetPort('') +
    ' -ReportPath "' + ReportFile + '" -StatusFile "' + InstallStatusFile + '"' +
    ' -HealthTimeoutSeconds {#HealthWaitSeconds}';
  if Region <> '' then
    Arguments := Arguments + ' -Region ' + Region;

  Log('Install script arguments: ' + Arguments);
  WizardForm.StatusLabel.Caption := CustomMessage('InstallingStep');
  WizardForm.ProgressGauge.Style := npbstMarquee;
  TimerId := SetTimer(0, 0, PollMilliseconds, CreateCallback(@PollStatus));
  try
    Failed := not Exec(ExpandConstant('{#PowerShellExe}'), Arguments, ExpandConstant('{tmp}'),
      SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0);
  finally
    KillTimer(0, TimerId);
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;

  LastLine := ReadLastStatusLine(InstallStatusFile);
  CopyReportForUser;
  Log('Install script: exit code ' + IntToStr(ResultCode) + ', last status line "' + LastLine + '", report ' + ReportFile);
  Ready := (not Failed) and (LastLine = '{#ReadyMarker}');
  if LastLine = '{#FailedMarker}' then
    Failed := True;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID <> wpFinished then
    Exit;
  if Failed then
    WizardForm.FinishedLabel.Caption := CustomMessage('FinishFailed') + #13#10 + ReportFile
  else if Ready then
    WizardForm.FinishedLabel.Caption := CustomMessage('FinishReady')
  else
    WizardForm.FinishedLabel.Caption := CustomMessage('FinishStarting');
end;

function GetCustomSetupExitCode: Integer;
begin
  if Failed then
    Result := ExitCodeInstallFailed
  else
    Result := 0;
end;

function AskRemoveData: Boolean;
begin
  Result := ParamPresent('REMOVEDATA');
  if (not Result) and (not UninstallSilent) then
    Result := SuppressibleMsgBox(CustomMessage('AskRemoveData'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES;
end;

procedure AbortUninstall(const UninstallReport: String);
var
  MessageText: String;
begin
  MessageText := CustomMessage('UninstallFailed') + #13#10#13#10;
  if FileExists(UninstallReport) then
    MessageText := MessageText + CustomMessage('UninstallFailedReport') + #13#10 + UninstallReport
  else
    MessageText := MessageText + CustomMessage('UninstallFailedContact');
  Log('Uninstallation aborted before removing anything; Klacks stays registered so that it can be uninstalled again.');
  if not UninstallSilent then
    SuppressibleMsgBox(MessageText, mbError, MB_OK, IDOK);
  Abort;
end;

{ Runs in usUninstall: after the user confirmed, before Inno removes files and the uninstall entry. Abort here ends
  the uninstaller with a non-zero exit code and leaves the installation registered (see the Abort topic of the help). }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Arguments, UninstallReport: String;
  ResultCode: Integer;
  Started: Boolean;
begin
  if CurUninstallStep <> usUninstall then
    Exit;
  UninstallReport := ExpandConstant('{app}\{#UninstallReportPrefix}') +
    GetDateTimeString('{#ReportTimestampFormat}', #0, #0) + '.txt';
  Arguments := '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\scripts\Uninstall-Klacks.ps1') + '"' +
    ' -LogPath "' + UninstallReport + '"';
  if AskRemoveData then
    Arguments := Arguments + ' -RemoveData';
  Log('Uninstall script arguments: ' + Arguments);
  Started := Exec(ExpandConstant('{#PowerShellExe}'), Arguments, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if not Started then
    Log('Uninstall script could not be started: ' + SysErrorMessage(ResultCode))
  else
    Log('Uninstall script exit code ' + IntToStr(ResultCode));
  if (not Started) or (ResultCode <> 0) then
    AbortUninstall(UninstallReport);
end;
