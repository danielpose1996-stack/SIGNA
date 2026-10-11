@echo off
setlocal
cd /d "%~dp0"
set "DOTNET_ROOT=%~dp0.tools\dotnet"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_GENERATE_ASPNET_CERTIFICATE=false"
set "GESTOR_DOCENTE_DEMO_DIR=%~dp0.demo-data"
if exist "%DOTNET_ROOT%\dotnet.exe" (
  "%DOTNET_ROOT%\dotnet.exe" run --project "%~dp0src\GestorDocente.Desktop" --no-restore
) else (
  dotnet run --project "%~dp0src\GestorDocente.Desktop"
)
if errorlevel 1 pause
endlocal
