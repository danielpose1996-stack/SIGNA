param(
    [string]$Version='1.6.0',
    [Parameter(Mandatory)][string]$Payload,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$Compiler=(Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe')
)
$ErrorActionPreference='Stop'
if($Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$'){throw 'Versión inválida'}
$payloadFile=Get-Item -LiteralPath $Payload
if($payloadFile.Length -lt 1000000){throw 'El Setup Velopack está incompleto'}
if($payloadFile.VersionInfo.FileDescription.Trim() -ne 'SIGNA' -or $payloadFile.VersionInfo.ProductName.Trim() -ne 'SIGNA'){
    throw 'Se requiere el Setup Velopack original de SIGNA, no un asistente ya envuelto'
}
if(-not (Test-Path -LiteralPath $Compiler)){throw 'Se requiere el compilador Inno Setup 6 aprobado'}
$repoRoot=Split-Path $PSScriptRoot -Parent
$destination=[System.IO.Path]::GetFullPath($OutputDirectory)
$output=Join-Path $destination "SIGNA-$Version-windows-x64-setup.exe"
if(Test-Path -LiteralPath $output){throw 'No se sobrescriben instaladores locales existentes'}
$sha=(Get-FileHash -LiteralPath $payloadFile.FullName -Algorithm SHA256).Hash
New-Item -ItemType Directory -Path $destination -Force | Out-Null
& $Compiler "/DPayloadPath=$($payloadFile.FullName)" "/DPayloadSHA256=$sha" "/DReleaseDir=$destination" "/DAppVersion=$Version" (Join-Path $repoRoot 'src/GestorDocente.Desktop/Packaging/Windows/SIGNA.iss')
if($LASTEXITCODE -ne 0){throw 'No se pudo compilar el asistente de instalación'}
if(-not (Test-Path -LiteralPath $output)){throw 'No se generó el instalador esperado'}
Get-FileHash -LiteralPath $output -Algorithm SHA256
