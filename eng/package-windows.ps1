param([string]$Version='1.0.0',[string]$Dotnet='dotnet',[string]$Python='python')
$ErrorActionPreference='Stop'
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw 'Usa una versión numérica mayor.menor.parche'}
$repoRoot=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repoRoot 'src/GestorDocente.Desktop/GestorDocente.Desktop.csproj'
$publish=Join-Path $repoRoot 'artifacts/packages/publish/win-x64'
$release=Join-Path $repoRoot 'artifacts/packages/release'
if(Test-Path $publish){throw 'El directorio de publicación ya existe; utiliza un checkout limpio para empaquetar'}
New-Item -ItemType Directory -Path $release -Force | Out-Null
& $Dotnet publish $project -c Release -r win-x64 --self-contained true -p:UseAppHost=true "-p:Version=$Version" '-p:NuGetLockFilePath=obj/release.packages.lock.json' -o $publish
if($LASTEXITCODE -ne 0){throw 'Falló la publicación Windows'}
& $Python (Join-Path $PSScriptRoot 'copy-licenses.py') $publish win-x64
if($LASTEXITCODE -ne 0){throw 'No se pudieron recopilar las licencias'}
$compiler=Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'
if(-not (Test-Path -LiteralPath $compiler)){throw 'Instala Inno Setup 6 desde su sitio oficial para compilar el instalador'}
& $compiler "/DPublishDir=$publish" "/DReleaseDir=$release" "/DAppVersion=$Version" (Join-Path $repoRoot 'src/GestorDocente.Desktop/Packaging/Windows/SIGNA.iss')
if($LASTEXITCODE -ne 0){throw 'Falló la compilación del instalador Windows'}
Get-FileHash -LiteralPath (Join-Path $release "SIGNA-$Version-windows-x64-setup.exe") -Algorithm SHA256
