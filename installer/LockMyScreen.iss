; Установщик LockMyScreen (Inno Setup 6).
; Собирать через build-installer.ps1 в корне проекта — он сначала собирает программу.

#define AppName "LockMyScreen"
#define AppExe "LockMyScreen.exe"
#define AppExeSource "..\bin\Release\net48\LockMyScreen.exe"
; Версия берётся из самого exe (свойство <Version> в LockMyScreen.csproj).
#define AppVersion GetStringFileInfo(AppExeSource, "ProductVersion")
; Должно совпадать с Program.MutexName.
#define AppMutexName "LockMyScreen-SingleInstance-5E1C7A2B"

[Setup]
; AppId никогда не менять — по нему Windows узнаёт, что это та же программа (для обновлений и удаления).
AppId={{0372D2C3-F4B4-431A-91E6-D5119460CAF7}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
; Установка для текущего пользователя, без прав администратора: %LocalAppData%\Programs\LockMyScreen
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\icons\app.ico
OutputDir=..\dist
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
MinVersion=10.0
; Язык мастера — по языку Windows, без лишнего вопроса.
ShowLanguageDialog=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[CustomMessages]
english.AutoStart=Start {#AppName} with Windows
russian.AutoStart=Запускать {#AppName} вместе с Windows
ukrainian.AutoStart=Запускати {#AppName} разом з Windows
english.DeleteSettings=Also delete {#AppName} settings (language, widget position)?
russian.DeleteSettings=Удалить также настройки {#AppName} (язык, положение виджета)?
ukrainian.DeleteSettings=Видалити також налаштування {#AppName} (мова, положення віджета)?

[Tasks]
Name: "autostart"; Description: "{cm:AutoStart}"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#AppExeSource}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; \
  ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// Просит запущенную программу закрыться (LockMyScreen.exe --exit) и ждёт до 5 секунд.
procedure CloseRunningApp(const ExePath: String);
var
  ResultCode, I: Integer;
begin
  if not CheckForMutexes('{#AppMutexName}') then Exit;
  if FileExists(ExePath) then
    Exec(ExePath, '--exit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  I := 0;
  while CheckForMutexes('{#AppMutexName}') and (I < 50) do
  begin
    Sleep(100);
    I := I + 1;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  // Берём новый exe из самого установщика — так получится закрыть программу,
  // даже если она запущена не из папки установки.
  ExtractTemporaryFile('{#AppExe}');
  CloseRunningApp(ExpandConstant('{tmp}\{#AppExe}'));
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  // При обновлении: если галочку автозапуска сняли — убираем старую запись.
  if (CurStep = ssPostInstall) and not WizardIsTaskSelected('autostart') then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#AppName}');
end;

function InitializeUninstall(): Boolean;
begin
  CloseRunningApp(ExpandConstant('{app}\{#AppExe}'));
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  SettingsDir: String;
begin
  if CurUninstallStep <> usPostUninstall then Exit;
  SettingsDir := ExpandConstant('{userappdata}\{#AppName}');
  if DirExists(SettingsDir) and not UninstallSilent then
    if MsgBox(CustomMessage('DeleteSettings'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      DelTree(SettingsDir, True, True, True);
end;
