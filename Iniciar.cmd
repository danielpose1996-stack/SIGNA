@echo off
setlocal
cd /d "%~dp0"
set "AULA_DOTNET=dotnet"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_GENERATE_ASPNET_CERTIFICATE=false"
set "GESTOR_DOCENTE_DEMO_DIR=%~dp0.demo-data"
if exist "%~dp0.tools\dotnet\dotnet.exe" (
  set "DOTNET_ROOT=%~dp0.tools\dotnet"
  set "AULA_DOTNET=%~dp0.tools\dotnet\dotnet.exe"
)
echo Preparando Gestor Academico Docente...
"%AULA_DOTNET%" build "%~dp0src\GestorDocente.Desktop" --no-incremental
if errorlevel 1 (
  echo No se pudo compilar la aplicacion. Revisa el error anterior.
  echo Si esta abierta, cierrala antes de volver a iniciar.
  pause
  exit /b 1
)
"%AULA_DOTNET%" run --project "%~dp0src\GestorDocente.Desktop" --no-build --no-restore
if errorlevel 1 pause
endlocal
