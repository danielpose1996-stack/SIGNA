param([string]$Version='1.6.0',[string]$Dotnet='dotnet',[string]$Python='python',[string]$ArtifactRoot='artifacts/packages')
$ErrorActionPreference='Stop'
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw 'Usa una versión numérica mayor.menor.parche'}
$repoRoot=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repoRoot 'src/GestorDocente.Desktop/GestorDocente.Desktop.csproj'
$packageRoot=Join-Path $repoRoot $ArtifactRoot
$publish=Join-Path $packageRoot 'publish/win-x64'
$release=Join-Path $packageRoot 'release'
$velopack=Join-Path $packageRoot 'velopack/win-x64'
if(Test-Path $publish){throw 'El directorio de publicación ya existe; utiliza un checkout limpio para empaquetar'}
New-Item -ItemType Directory -Path $release -Force | Out-Null
& $Dotnet publish $project -c Release -r win-x64 --self-contained true -p:UseAppHost=true "-p:Version=$Version" '-p:NuGetLockFilePath=obj/release.packages.lock.json' -o $publish
if($LASTEXITCODE -ne 0){throw 'Falló la publicación Windows'}
& $Python (Join-Path $PSScriptRoot 'copy-licenses.py') $publish win-x64
if($LASTEXITCODE -ne 0){throw 'No se pudieron recopilar las licencias'}
& $Dotnet tool restore
if($LASTEXITCODE -ne 0){throw 'No se pudo restaurar la herramienta Velopack fijada'}
& $Dotnet tool run vpk -- --skip-updates --yes pack --packId SIGNA --packVersion $Version --packDir $publish --mainExe GestorDocente.Desktop.exe --runtime win-x64 --channel win-x64 --packTitle SIGNA --packAuthors SIGNA --icon (Join-Path $repoRoot 'src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_windows.ico') --outputDir $velopack --noPortable
if($LASTEXITCODE -ne 0){throw 'Falló el empaquetado Velopack Windows'}
& $Python (Join-Path $PSScriptRoot 'collect-velopack.py') $velopack $release win-x64 $Version
if($LASTEXITCODE -ne 0){throw 'El feed o paquete de actualización Windows no es válido'}
$setups=@(Get-ChildItem -LiteralPath $velopack -Filter '*Setup.exe')
if($setups.Count -ne 1){throw 'No se encontró un único instalador Velopack'}
$installer=Join-Path $release "SIGNA-$Version-windows-x64-setup.exe"
if(Test-Path -LiteralPath $installer){throw 'No se sobrescriben instaladores existentes'}
& (Join-Path $PSScriptRoot 'wrap-windows-installer.ps1') -Version $Version -Payload $setups[0].FullName -OutputDirectory $release
Get-FileHash -LiteralPath (Join-Path $release "SIGNA-$Version-windows-x64-setup.exe") -Algorithm SHA256
