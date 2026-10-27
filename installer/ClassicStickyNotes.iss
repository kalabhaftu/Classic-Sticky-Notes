; Inno Setup Script for Classic Sticky Notes
; Built with Inno Setup 6

#define MyAppName "Classic Sticky Notes"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Antigravity"
#define MyAppExeName "ClassicStickyNotes.exe"

[Setup]
AppId={{E68A8D2A-0A7E-4E65-B8AE-4591F44E72F1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\installer_output
OutputBaseFilename=ClassicStickyNotes_Setup_v{#MyAppVersion}
SetupIconFile=..\icon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes
ChangesEnvironment=yes
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupentry"; Description: "Launch Classic Sticky Notes when Windows starts"; GroupDescription: "Windows Startup:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Top-level Start Menu entry (ensures Windows Search indexes it as an App immediately)
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"
; Program group entry
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
; Optional desktop icon
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

[Registry]
; Windows Startup
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ClassicStickyNotes"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: startupentry; Flags: uninsdeletevalue

; App Paths (registers executable with Windows Search and Win+R Run dialog)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\Classic Sticky Notes.exe"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\Classic Sticky Notes.exe"; ValueType: string; ValueName: "Path"; ValueData: "{app}"; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
