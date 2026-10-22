; Asistente de destino/licencia. Velopack conserva instalación y desinstalación.
#ifndef PayloadPath
  #error Se requiere PayloadPath
#endif
#ifndef PayloadSHA256
  #error Se requiere PayloadSHA256
#endif
#ifndef ReleaseDir
  #error Se requiere ReleaseDir
#endif
#ifndef AppVersion
  #define AppVersion "1.6.0"
#endif
#define ProjectRoot "..\..\..\.."

[Setup]
AppId=SIGNA-FolderWizard
AppName=SIGNA
AppVersion={#AppVersion}
AppPublisher=SIGNA
AppPublisherURL=https://github.com/danielpose1996-stack/SIGNA
DefaultDirName={localappdata}\SIGNA
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableDirPage=no
DisableWelcomePage=no
DisableReadyPage=no
DisableProgramGroupPage=yes
AlwaysShowDirOnReadyPage=yes
UsePreviousAppDir=no
Uninstallable=no
CreateUninstallRegKey=no
AllowUNCPath=no
LicenseFile={#ProjectRoot}\LICENSE
OutputDir={#ReleaseDir}
OutputBaseFilename=SIGNA-{#AppVersion}-windows-x64-setup
SetupIconFile=..\..\Assets\Branding\SIGNA_icono_windows.ico
Compression=none
WizardStyle=modern
CloseApplications=no
RestartApplications=no
ExtraDiskSpaceRequired=300000000

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PayloadPath}"; DestName: "SIGNA-Velopack-Setup.exe"; Flags: dontcopy

[Run]
Filename: "{app}\GestorDocente.Desktop.exe"; Description: "Abrir SIGNA"; Flags: nowait postinstall skipifsilent

[Code]
const
  ReparsePoint = $400;

function FileAttributes(Name: String): LongWord;
  external 'GetFileAttributesW@kernel32.dll stdcall';

function NormalPath(Value: String): String;
begin
  Result := RemoveBackslashUnlessRoot(ExpandFileName(Value));
end;

function WithinPath(Value, Parent: String): Boolean;
begin
  Result := Pos(Lowercase(AddBackslash(NormalPath(Parent))), Lowercase(AddBackslash(NormalPath(Value)))) = 1;
end;

function Element(Text, Tag: String): String;
var StartPos, EndPos: Integer;
begin
  Result := '';
  StartPos := Pos('<' + Tag + '>', Text);
  if StartPos = 0 then Exit;
  Text := Copy(Text, StartPos + Length(Tag) + 2, Length(Text));
  EndPos := Pos('</' + Tag + '>', Text);
  if EndPos > 0 then Result := Copy(Text, 1, EndPos - 1);
end;

function ValidPreviousVersion(Value: String): Boolean;
var Existing, Target: TArrayOfString; Index, Left, Right: Integer;
begin
  Result := False;
  Existing := StringSplit(Value, ['.'], stAll);
  Target := StringSplit('{#AppVersion}', ['.'], stAll);
  if (GetArrayLength(Existing) <> 3) or (GetArrayLength(Target) <> 3) then Exit;
  for Index := 0 to 2 do begin
    Left := StrToIntDef(Existing[Index], -1);
    Right := StrToIntDef(Target[Index], -1);
    if (Left < 0) or (Right < 0) then Exit;
  end;
  for Index := 0 to 2 do begin
    Left := StrToIntDef(Existing[Index], -1);
    Right := StrToIntDef(Target[Index], -1);
    if Left > Right then Exit;
    if Left < Right then begin Result := True; Exit; end;
  end;
  Result := True;
end;

function InspectFiles(Dir: String; Root: Boolean; Depth: Integer): String;
var Item: TFindRec; Name, Extension: String;
begin
  Result := '';
  if Depth > 16 then begin Result := 'La carpeta contiene una estructura ajena al programa.'; Exit; end;
  if not FindFirst(AddBackslash(Dir) + '*', Item) then begin
    Result := 'No se pudo revisar la carpeta. Elige un destino con permisos de lectura y escritura.'; Exit;
  end;
  try
    repeat
      Name := Item.Name;
      if (Name <> '.') and (Name <> '..') then begin
        if (Item.Attributes and ReparsePoint) <> 0 then begin
          Result := 'El destino contiene enlaces de carpetas o archivos. Elige una carpeta local sin enlaces.'; Exit;
        end;
        if (Item.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then begin
          if Root and (CompareText(Name, 'current') <> 0) and (CompareText(Name, 'packages') <> 0) then begin
            Result := 'La carpeta contiene archivos ajenos a SIGNA. Selecciona una carpeta vacía.'; Exit;
          end;
          Result := InspectFiles(AddBackslash(Dir) + Name, False, Depth + 1);
          if Result <> '' then Exit;
        end else begin
          if Root and (CompareText(Name, 'Update.exe') <> 0) and (CompareText(Name, 'GestorDocente.Desktop.exe') <> 0) then begin
            Result := 'La carpeta contiene archivos ajenos a SIGNA. Selecciona una carpeta vacía.'; Exit;
          end;
          Extension := Lowercase(ExtractFileExt(Name));
          if (Extension = '.db') or (Extension = '.sqlite') or (Extension = '.sqlite3') or
             (Extension = '.db3') or (Extension = '.s3db') or (Extension = '.backup') or
             (Extension = '.bak') or (Extension = '.db-wal') or (Extension = '.db-shm') or
             (Extension = '.sqlite-wal') or (Extension = '.sqlite3-wal') or
             (Pos('.aulabk', Lowercase(Name)) > 0) then begin
            Result := 'Hay datos o respaldos dentro de la carpeta del programa. Consérvalos fuera y vuelve a intentar. No se modificó ningún archivo.'; Exit;
          end;
        end;
      end;
    until not FindNext(Item);
  finally
    FindClose(Item);
  end;
end;

function HasEntries(Dir: String): Boolean;
var Item: TFindRec;
begin
  Result := False;
  if not FindFirst(AddBackslash(Dir) + '*', Item) then Exit;
  try
    repeat
      if (Item.Name <> '.') and (Item.Name <> '..') then begin Result := True; Exit; end;
    until not FindNext(Item);
  finally FindClose(Item); end;
end;

function ValidateDestination(Dir: String): String;
var Parent, Previous: String; Manifest: AnsiString; Probe: TFindRec;
begin
  Result := '';
  Dir := NormalPath(Dir);
  if (Length(Dir) < 4) or (CompareText(Dir, AddBackslash(ExtractFileDrive(Dir))) = 0) or
     (CompareText(Dir, GetEnv('USERPROFILE')) = 0) or
     (CompareText(Dir, ExpandConstant('{userdocs}')) = 0) or
     (CompareText(Dir, ExpandConstant('{userdesktop}')) = 0) or
     (CompareText(Dir, ExpandConstant('{localappdata}')) = 0) or
     (CompareText(Dir, ExpandConstant('{userappdata}')) = 0) or
     WithinPath(Dir, ExpandConstant('{win}')) or
     WithinPath(Dir, ExpandConstant('{localappdata}\AulaRegistro')) then begin
    Result := 'Elige una carpeta propia para SIGNA, separada de tus documentos y de la base de datos.'; Exit;
  end;
  Parent := Dir;
  repeat
    if DirExists(Parent) and ((FileAttributes(Parent) and ReparsePoint) <> 0) then begin
      Result := 'La ruta contiene un enlace de carpeta. Elige un destino local sin enlaces.'; Exit;
    end;
    Previous := Parent; Parent := ExtractFileDir(Parent);
  until CompareText(Parent, Previous) = 0;
  if not DirExists(Dir) then Exit;
  if not FindFirst(AddBackslash(Dir) + '*', Probe) then begin
    Result := 'No se pudo revisar la carpeta. Elige un destino con permisos de lectura y escritura.'; Exit;
  end;
  FindClose(Probe);
  if not HasEntries(Dir) then Exit;
  if not LoadStringFromFile(AddBackslash(Dir) + 'current\sq.version', Manifest) then begin
    Result := 'Esta carpeta no está vacía y no es una instalación de SIGNA. Elige una carpeta vacía; no se reemplazaron tus archivos.'; Exit;
  end;
  if (Element(String(Manifest), 'id') <> 'SIGNA') or (Element(String(Manifest), 'channel') <> 'win-x64') or
     not FileExists(AddBackslash(Dir) + 'Update.exe') or
     not FileExists(AddBackslash(Dir) + 'current\GestorDocente.Desktop.exe') then begin
    Result := 'La carpeta no corresponde a una instalación compatible de SIGNA. Elige una carpeta vacía.'; Exit;
  end;
  if not ValidPreviousVersion(Element(String(Manifest), 'version')) then begin
    Result := 'La carpeta contiene una versión más nueva o incompatible. No se permite reemplazarla por una anterior.'; Exit;
  end;
  Result := InspectFiles(Dir, True, 0);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var Error: String;
begin
  Result := True;
  if CurPageID = wpSelectDir then begin
    Error := ValidateDestination(WizardDirValue);
    if Error <> '' then begin
      Log(Error);
      if not WizardSilent then MsgBox(Error, mbError, MB_OK);
      Result := False; Exit;
    end;
    if DirExists(WizardDirValue) and HasEntries(WizardDirValue) and not WizardSilent then
      Result := MsgBox('Se repararán los archivos de SIGNA en esta carpeta. Cierra SIGNA antes de continuar. Tus datos y respaldos deben permanecer fuera del programa. ¿Deseas continuar?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var ExitCode: Integer; Payload: String;
begin
  Result := ValidateDestination(WizardDirValue);
  if Result <> '' then Exit;
  ExtractTemporaryFile('SIGNA-Velopack-Setup.exe');
  Payload := ExpandConstant('{tmp}\SIGNA-Velopack-Setup.exe');
  if CompareText(GetSHA256OfFile(Payload), '{#PayloadSHA256}') <> 0 then begin
    Result := 'El instalador no supera la verificación de integridad. Descárgalo otra vez desde la página oficial.'; Exit;
  end;
  WizardForm.CancelButton.Enabled := False;
  try
    if not Exec(Payload, '--silent --installto "' + WizardDirValue + '"', '', SW_HIDE, ewWaitUntilTerminated, ExitCode) then
      Result := 'No se pudo iniciar la instalación de SIGNA. Revisa los permisos de la carpeta e intenta de nuevo.'
    else if ExitCode <> 0 then
      Result := 'No se pudo completar la instalación de SIGNA (código ' + IntToStr(ExitCode) + '). Revisa permisos, espacio libre y que SIGNA esté cerrado.'
    else if not FileExists(AddBackslash(WizardDirValue) + 'current\sq.version') or
            not FileExists(AddBackslash(WizardDirValue) + 'current\GestorDocente.Desktop.exe') or
            not FileExists(AddBackslash(WizardDirValue) + 'Update.exe') then
      Result := 'La instalación no generó todos los archivos necesarios. No se confirmó una instalación exitosa.';
  finally
    WizardForm.CancelButton.Enabled := True;
  end;
end;
