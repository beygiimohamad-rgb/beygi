#define MyAppName "Parsis AutoTrader"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Parsis"
#define MyAppExeName "Parsis.AutoTrader.App.exe"
[Setup]
AppId={{49AB13C2-3975-4DDE-9F11-4D464811F809}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Parsis AutoTrader
DefaultGroupName=Parsis AutoTrader
OutputDir=output
OutputBaseFilename=ParsisAutoTraderSetup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=assets\app.ico
[Files]
Source: "..\artifacts\client\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\pishniaz\*"; DestDir: "{app}\pishniaz"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autodesktop}\Parsis AutoTrader"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autoprograms}\Parsis AutoTrader"; Filename: "{app}\{#MyAppExeName}"
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: checkedonce
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Run Parsis AutoTrader"; Flags: nowait postinstall skipifsilent
