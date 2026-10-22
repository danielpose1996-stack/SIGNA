param([string]$Version='1.6.1',[string]$Installer)
$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true'){throw 'La instalación de prueba solo se permite en el runner efímero de GitHub'}
$repoRoot=Split-Path $PSScriptRoot -Parent
if(-not $Installer){$Installer=Join-Path $repoRoot "artifacts/packages/release/SIGNA-$Version-windows-x64-setup.exe"}
$installer=(Get-Item -LiteralPath $Installer).FullName
$installDir=Join-Path $env:RUNNER_TEMP 'SIGNA carpeta elegida con espacios y ñ'
if(-not ([System.IO.Path]::GetFullPath($installDir)).StartsWith(([System.IO.Path]::GetFullPath($env:RUNNER_TEMP)).TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'El destino de prueba no pertenece al runner'}
function Invoke-Wizard([string]$Directory,[string]$LogName){
    $log=Join-Path $env:RUNNER_TEMP $LogName
    $process=Start-Process -FilePath $installer -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',"/DIR=`"$Directory`"","/LOG=`"$log`"") -WindowStyle Hidden -Wait -PassThru
    return $process.ExitCode
}
$dataDir=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'AulaRegistro'
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
$sentinel=Join-Path $dataDir 'prueba-empaquetado.txt'
if(Test-Path -LiteralPath $sentinel){throw 'El fixture de datos ya existe'}
Set-Content -LiteralPath $sentinel -Value 'Datos ficticios que deben conservarse'
$before=(Get-FileHash -LiteralPath $sentinel).Hash
$exitCode=Invoke-Wizard $installDir 'SIGNA-instalar.log'
if($exitCode -ne 0){Get-Content (Join-Path $env:RUNNER_TEMP 'SIGNA-instalar.log') -Tail 20;throw "Falló la instalación en carpeta elegida: $exitCode"}
$current=Join-Path $installDir 'current'
foreach($file in @('GestorDocente.Desktop.exe','GestorDocente.Desktop.dll','e_sqlite3.dll','libSkiaSharp.dll','LICENSE','Licenses/DEPENDENCIES.md','sq.version')){
    if(-not (Test-Path -LiteralPath (Join-Path $current $file))){throw "Falta $file en la instalación"}
}
if(-not (Test-Path -LiteralPath (Join-Path $installDir 'Update.exe'))){throw 'Falta el actualizador Velopack'}
if(-not (Test-Path -LiteralPath (Join-Path $installDir 'SIGNA.exe'))){throw 'Falta el acceso estable de Velopack'}
if(Test-Path -LiteralPath (Join-Path $installDir 'unins000.exe')){throw 'El asistente creó un desinstalador duplicado'}
if((Get-Item (Join-Path $current 'GestorDocente.Desktop.exe')).VersionInfo.ProductName -ne 'SIGNA'){throw 'Identidad incorrecta'}
$versionOutput=Join-Path $env:RUNNER_TEMP 'SIGNA-version.txt'
$probe=Start-Process -FilePath (Join-Path $current 'GestorDocente.Desktop.exe') -ArgumentList '--version' -RedirectStandardOutput $versionOutput -WindowStyle Hidden -Wait -PassThru
if($probe.ExitCode -ne 0 -or (Get-Content -LiteralPath $versionOutput -Raw) -notmatch "SIGNA $([regex]::Escape($Version))"){throw 'El runtime del programa instalado no inicia correctamente'}
# Los errores de selección no deben reemplazar datos ni producir un falso éxito.
$foreign=Join-Path $env:RUNNER_TEMP 'Documentos ficticios del docente'
New-Item -ItemType Directory -Path $foreign | Out-Null
$foreignFile=Join-Path $foreign 'material.txt';Set-Content -LiteralPath $foreignFile -Value 'Contenido ficticio que debe conservarse'
$foreignHash=(Get-FileHash -LiteralPath $foreignFile).Hash
if((Invoke-Wizard $foreign 'SIGNA-rechazo-ajena.log') -eq 0){throw 'Se admitió una carpeta ajena no vacía'}
if((Get-FileHash -LiteralPath $foreignFile).Hash -ne $foreignHash){throw 'El rechazo modificó una carpeta ajena'}
if((Invoke-Wizard $dataDir 'SIGNA-rechazo-datos.log') -eq 0){throw 'Se admitió la carpeta de datos académicos'}
$junction=Join-Path $env:RUNNER_TEMP 'SIGNA-enlace-prueba'
New-Item -ItemType Junction -Path $junction -Target $foreign | Out-Null
if((Invoke-Wizard (Join-Path $junction 'SIGNA') 'SIGNA-rechazo-enlace.log') -eq 0){throw 'Se admitió una ruta con enlaces'}
$embeddedData=Join-Path $current 'datos-ficticios.db';Set-Content -LiteralPath $embeddedData -Value 'Datos ficticios dentro de carpeta incorrecta'
$embeddedHash=(Get-FileHash -LiteralPath $embeddedData).Hash
if((Invoke-Wizard $installDir 'SIGNA-rechazo-db.log') -eq 0){throw 'Se admitió reparar con datos dentro del programa'}
if((Get-FileHash -LiteralPath $embeddedData).Hash -ne $embeddedHash){throw 'El rechazo modificó datos del fixture'}
Remove-Item -LiteralPath $embeddedData
$manifest=Join-Path $current 'sq.version';$manifestBytes=[System.IO.File]::ReadAllBytes($manifest)
$text=[System.Text.Encoding]::UTF8.GetString($manifestBytes).Replace("<version>$Version</version>",'<version>99.0.0</version>')
[System.IO.File]::WriteAllText($manifest,$text,[System.Text.UTF8Encoding]::new($false))
if((Invoke-Wizard $installDir 'SIGNA-rechazo-downgrade.log') -eq 0){throw 'Se admitió reemplazar una versión superior'}
$text=[System.Text.Encoding]::UTF8.GetString($manifestBytes).Replace("<version>$Version</version>",'<version>0.invalido.0</version>')
[System.IO.File]::WriteAllText($manifest,$text,[System.Text.UTF8Encoding]::new($false))
if((Invoke-Wizard $installDir 'SIGNA-rechazo-version-invalida.log') -eq 0){throw 'Se admitió un manifiesto incompatible'}
[System.IO.File]::WriteAllBytes($manifest,$manifestBytes)
if((Invoke-Wizard $installDir 'SIGNA-reparar.log') -ne 0){Get-Content (Join-Path $env:RUNNER_TEMP 'SIGNA-reparar.log') -Tail 25;throw 'La reparación compatible no funciona'}
if((Get-FileHash -LiteralPath $sentinel).Hash -ne $before){throw 'La reparación alteró datos externos'}
$process=Start-Process -FilePath (Join-Path $installDir 'Update.exe') -ArgumentList 'uninstall','--silent' -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw 'Falló la desinstalación de prueba'}
if((Get-FileHash -LiteralPath $sentinel).Hash -ne $before){throw 'La desinstalación alteró los datos del usuario'}
if(Test-Path -LiteralPath (Join-Path $current 'GestorDocente.Desktop.exe')){throw 'El programa no se desinstaló'}
Write-Output 'Selección de carpeta Unicode, instalación, reparación, rechazos protectores y desinstalación correctos; datos conservados. No se inició la interfaz.'
