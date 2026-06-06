; Inno Setup Script for We Share
; Standalone Windows Installer — .NET 8 Self-Contained (no runtime required on target PC)

#define AppName      "We Share"
#define AppVersion   "1.0.0"
#define AppPublisher "Sowmiyan-S"
#define AppURL       "https://github.com/sowmiyan-s/We-Share"
#define AppExeName   "WeShare.Desktop.exe"
#define AppIcon      "src\WeShare.UI\Assets\logo.ico"

[Setup]
AppId={{C7A9E5B2-D4A1-4F9C-B5A1-9E2F8B7D6C5A}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
AppCopyright=© 2026 {#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=setup
OutputBaseFilename=WeShare_Setup_{#AppVersion}
SetupIconFile={#AppIcon}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
; Installer banner (164 × 314 px BMP) and small logo (55 × 58 px BMP)
WizardImageFile=src\WeShare.UI\Assets\Design\installer_banner_light.bmp
WizardSmallImageFile=src\WeShare.UI\Assets\logo_light.bmp
Compression=lzma2/ultra64
InternalCompressLevel=max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExeName}
DisableWelcomePage=no
WizardImageStretch=yes
; Minimum Windows 10 (build 19041 — required by .NET 8 WinRT APIs used by app)
MinVersion=10.0.19041

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Launch We Share on Windows startup"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Self-contained publish output — embeds the .NET 8 runtime, no separate install needed
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "src\WeShare.UI\Assets\logo.ico";  DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "src\WeShare.UI\Assets\logo.png";  DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";              Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}";        Filename: "{app}\{#AppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#AppExeName}"
Name: "{commonstartup}\{#AppName}";      Filename: "{app}\{#AppExeName}"; Tasks: startupicon
Name: "{group}\Uninstall {#AppName}";    Filename: "{uninstallexe}"

[Run]
; Launch app after install
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
; Firewall — allow inbound + outbound so peer discovery and TCP transfer work
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""{#AppName}"" dir=in  action=allow program=""{app}\{#AppExeName}"" enable=yes profile=any"; Flags: runhidden
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""{#AppName}"" dir=out action=allow program=""{app}\{#AppExeName}"" enable=yes profile=any"; Flags: runhidden

[UninstallRun]
; Kill running process before deleting files
Filename: "taskkill.exe"; Parameters: "/f /im WeShare.Desktop.exe"; RunOnceId: "KillApp"; Flags: runhidden
; Remove firewall rules on uninstall
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#AppName}"""; RunOnceId: "RemoveFirewallRule"; Flags: runhidden

[Code]
var
  BaseColor:    TColor;
  TextColor:    TColor;
  SubTextColor: TColor;

procedure InitializeWizard;
begin
  BaseColor    := clWhite;
  TextColor    := $1A1A1A;   // Near-black
  SubTextColor := $666666;   // Mid-grey

  WizardForm.Color                            := BaseColor;
  WizardForm.InnerPage.Color                  := BaseColor;
  WizardForm.WelcomePage.Color                := BaseColor;
  WizardForm.FinishedPage.Color               := BaseColor;
  WizardForm.MainPanel.Color                  := BaseColor;

  WizardForm.PageNameLabel.Font.Color         := TextColor;
  WizardForm.PageNameLabel.Font.Name          := 'Segoe UI Semibold';
  WizardForm.PageNameLabel.Font.Size          := 12;
  WizardForm.PageDescriptionLabel.Font.Color  := SubTextColor;
  WizardForm.PageDescriptionLabel.Font.Name   := 'Segoe UI';

  WizardForm.WelcomeLabel1.Font.Color         := TextColor;
  WizardForm.WelcomeLabel1.Font.Size          := 16;
  WizardForm.WelcomeLabel2.Font.Color         := SubTextColor;

  WizardForm.FinishedHeadingLabel.Font.Color  := TextColor;
  WizardForm.FinishedHeadingLabel.Font.Size   := 16;
  WizardForm.FinishedLabel.Font.Color         := SubTextColor;

  WizardForm.SelectDirLabel.Font.Color        := TextColor;
  WizardForm.SelectDirBrowseLabel.Font.Color  := SubTextColor;
  WizardForm.SelectTasksLabel.Font.Color      := TextColor;
  WizardForm.ReadyLabel.Font.Color            := TextColor;
  WizardForm.ReadyMemo.Color                  := $FAFAFA;
  WizardForm.ReadyMemo.Font.Color             := SubTextColor;
  WizardForm.StatusLabel.Font.Color           := TextColor;
  WizardForm.FileNameLabel.Font.Color         := SubTextColor;

  WizardForm.BackButton.Cursor                := crHand;
  WizardForm.NextButton.Cursor                := crHand;
  WizardForm.CancelButton.Cursor              := crHand;

  WizardForm.Bevel.Visible                    := False;
  WizardForm.Bevel1.Visible                   := False;

  WizardForm.WizardBitmapImage.Stretch        := True;
  WizardForm.WizardBitmapImage2.Stretch       := True;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  WizardForm.InnerPage.Color  := BaseColor;
  WizardForm.MainPanel.Color  := BaseColor;

  if CurPageID = wpSelectTasks then
  begin
    WizardForm.TasksList.Color           := BaseColor;
    WizardForm.TasksList.Font.Color      := TextColor;
  end;

  if CurPageID = wpSelectDir then
  begin
    WizardForm.DirEdit.Color             := BaseColor;
    WizardForm.DirEdit.Font.Color        := TextColor;
  end;

  if CurPageID = wpSelectProgramGroup then
  begin
    WizardForm.GroupEdit.Color           := BaseColor;
    WizardForm.GroupEdit.Font.Color      := TextColor;
  end;
end;

{ ── Windows 10 version check ─────────────────────────────────────────────── }
{ We Share requires Windows 10 build 19041+ (same requirement as .NET 8).     }
{ The MinVersion directive handles this at OS level, but we add a friendly     }
{ message here in case Inno Setup itself is compiled with an older target.    }
function InitializeSetup: Boolean;
var
  WinVer: TWindowsVersion;
  UninstallerPath: string;
  ResultCode: Integer;
begin
  Result := True;

  GetWindowsVersionEx(WinVer);

  if (WinVer.Major < 10) or ((WinVer.Major = 10) and (WinVer.Build < 19041)) then
  begin
    MsgBox(
      'We Share requires Windows 10 version 2004 (build 19041) or later.' + #13#10 +
      'Please update Windows and try again.',
      mbError, MB_OK
    );
    Result := False;
    Exit;
  end;

  // Detect previous installation and prompt for clean install
  if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{C7A9E5B2-D4A1-4F9C-B5A1-9E2F8B7D6C5A}_is1', 'UninstallString', UninstallerPath) or
     RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{C7A9E5B2-D4A1-4F9C-B5A1-9E2F8B7D6C5A}_is1', 'UninstallString', UninstallerPath) then
  begin
    if MsgBox('We Share is already installed on this computer.' + #13#10 + #13#10 +
              'Would you like to perform a clean installation? (This uninstalls the previous version before installing, keeping your history database.)',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      UninstallerPath := RemoveQuotes(UninstallerPath);
      Exec(UninstallerPath, '/SILENT /NORESTART', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    end;
  end;
end;

function PrepareToInstall(var NeedsReboot: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  // Force-kill the app before extracting files to avoid file-in-use locks
  Exec('taskkill.exe', '/f /im WeShare.Desktop.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
