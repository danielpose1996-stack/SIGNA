param([string]$Version='1.0.0')
$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true'){throw 'La instalación de prueba solo se permite en el runner efímero de GitHub'}
$repoRoot=Split-Path $PSScriptRoot -Parent
$installer=Join-Path $repoRoot "artifacts/packages/release/SIGNA-$Version-windows-x64-setup.exe"
$installDir=Join-Path $env:RUNNER_TEMP 'SIGNA-instalacion-prueba'
$dataDir=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'AulaRegistro'
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
$sentinel=Join-Path $dataDir 'prueba-empaquetado.txt'
if(Test-Path -LiteralPath $sentinel){throw 'El fixture de datos ya existe'}
Set-Content -LiteralPath $sentinel -Value 'Datos ficticios que deben conservarse'
$before=(Get-FileHash -LiteralPath $sentinel).Hash
$process=Start-Process -FilePath $installer -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',"/DIR=`"$installDir`"") -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw "Falló la instalación: $($process.ExitCode)"}
foreach($file in @('GestorDocente.Desktop.exe','GestorDocente.Desktop.dll','e_sqlite3.dll','libSkiaSharp.dll','LICENSE','Licenses/DEPENDENCIES.md','unins000.exe')){
    if(-not (Test-Path -LiteralPath (Join-Path $installDir $file))){throw "Falta $file en la instalación"}
}
$original=(Get-FileHash (Join-Path $repoRoot 'src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_windows.ico')).Hash
if((Get-FileHash (Join-Path $installDir 'SIGNA.ico')).Hash -ne $original){throw 'El icono instalado difiere del oficial'}
if((Get-Item (Join-Path $installDir 'GestorDocente.Desktop.exe')).VersionInfo.ProductName -ne 'SIGNA'){throw 'Identidad incorrecta'}
$versionOutput=Join-Path $env:RUNNER_TEMP 'SIGNA-version.txt'
$probe=Start-Process -FilePath (Join-Path $installDir 'GestorDocente.Desktop.exe') -ArgumentList '--version' -RedirectStandardOutput $versionOutput -WindowStyle Hidden -Wait -PassThru
if($probe.ExitCode -ne 0 -or (Get-Content -LiteralPath $versionOutput -Raw) -notmatch "SIGNA $([regex]::Escape($Version))"){throw 'El runtime del programa instalado no inicia correctamente'}
$process=Start-Process -FilePath (Join-Path $installDir 'unins000.exe') -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART' -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw 'Falló la desinstalación de prueba'}
if((Get-FileHash -LiteralPath $sentinel).Hash -ne $before){throw 'La desinstalación alteró los datos del usuario'}
if(Test-Path -LiteralPath (Join-Path $installDir 'GestorDocente.Desktop.exe')){throw 'El programa no se desinstaló'}
Write-Output 'Instalación y desinstalación correctas; datos conservados. No se inició la aplicación.'
