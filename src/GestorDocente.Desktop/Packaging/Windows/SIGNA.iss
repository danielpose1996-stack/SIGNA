#ifndef PublishDir
  #error Se requiere PublishDir
#endif
#ifndef ReleaseDir
  #error Se requiere ReleaseDir
#endif
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define ProjectRoot "..\..\..\.."

[Setup]
AppId={{B31879E5-1EA1-48F7-AAF6-F85DE8107621}
AppName=SIGNA
AppVersion={#AppVersion}
AppPublisher=SIGNA
AppPublisherURL=https://github.com/danielpose1996-stack/SIGNA
AppSupportURL=https://github.com/danielpose1996-stack/SIGNA/issues
DefaultDirName={localappdata}\Programs\SIGNA
DefaultGroupName=SIGNA
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes
LicenseFile={#ProjectRoot}\LICENSE
OutputDir={#ReleaseDir}
OutputBaseFilename=SIGNA-{#AppVersion}-windows-x64-setup
SetupIconFile=..\..\Assets\Branding\SIGNA_icono_windows.ico
UninstallDisplayIcon={app}\SIGNA.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.db,*.sqlite,*.sqlite3,*.aulabk"
Source: "..\..\Assets\Branding\SIGNA_icono_windows.ico"; DestDir: "{app}"; DestName: "SIGNA.ico"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\SIGNA"; Filename: "{app}\GestorDocente.Desktop.exe"; IconFilename: "{app}\SIGNA.ico"
Name: "{autodesktop}\SIGNA"; Filename: "{app}\GestorDocente.Desktop.exe"; IconFilename: "{app}\SIGNA.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\GestorDocente.Desktop.exe"; Description: "Abrir SIGNA"; Flags: nowait postinstall skipifsilent
