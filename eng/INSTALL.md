# Instalar SIGNA 1.6.1

Soporte actual: **Windows 10/11 x64, macOS 15 o posterior Intel/Apple Silicon y Ubuntu 24.04 LTS x64/ARM64**. Descarga desde [Releases oficiales](https://github.com/danielpose1996-stack/SIGNA/releases/latest). Los instaladores incluyen .NET; no necesitas el SDK.

Desde SIGNA 1.6.0 instalado con Velopack puedes utilizar **Buscar actualizaciones → Actualizar SIGNA** cuando 1.6.1 esté publicada como estable. Las ediciones hasta 1.5.0 requieren instalar una vez la edición actualizable. Crea antes un respaldo local cifrado y conserva su contraseña. Consulta [el procedimiento completo](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).

| Equipo | Archivo |
| --- | --- |
| Windows 10/11 x64 | `SIGNA-1.6.1-windows-x64-setup.exe` |
| Ubuntu 24.04 LTS x64, actualización integrada | `SIGNA-1.6.1-linux-x64.AppImage` |
| Ubuntu 24.04 LTS ARM64, actualización integrada | `SIGNA-1.6.1-linux-arm64.AppImage` |
| Ubuntu 24.04 LTS x64, instalación con apt | `SIGNA-1.6.1-linux-x64.deb` |
| Ubuntu 24.04 LTS ARM64, instalación con apt | `SIGNA-1.6.1-linux-arm64.deb` |
| macOS 15+, Intel | `SIGNA-1.6.1-osx-x64.dmg` |
| macOS 15+, Apple Silicon | `SIGNA-1.6.1-osx-arm64.dmg` |

**Windows:** abre el EXE, revisa la licencia MIT y elige una carpeta. La opción predeterminada es `%LOCALAPPDATA%\SIGNA`; puedes elegir otra carpeta local con permisos de escritura. Utiliza una carpeta vacía o una instalación SIGNA Velopack compatible; el asistente rechaza archivos ajenos, bases y respaldos dentro del destino. Instala para tu usuario, sin administrador, con accesos en Inicio y escritorio. Velopack conserva actualizaciones y desinstalación; para retirar SIGNA utiliza Aplicaciones instaladas. Los datos del docente permanecen fuera del programa.

**macOS:** abre el DMG de tu arquitectura, arrastra SIGNA a Applications y confirma el reemplazo si corresponde. Expulsa el DMG y abre SIGNA desde Aplicaciones. Eliminar SIGNA.app conserva la base de datos local.

**Ubuntu con AppImage:** guarda la AppImage de tu arquitectura en una carpeta propia estable y escribible. Ejecuta `chmod +x SIGNA-1.6.1-linux-x64.AppImage` y ábrela; cambia el nombre para ARM64. Requiere soporte FUSE y las bibliotecas de .NET/Avalonia; en Wayland necesita XWayland. Esta edición permite **Buscar actualizaciones → Actualizar SIGNA**. No cambies su ubicación mientras se actualiza.

**Ubuntu con .deb:** ejecuta `sudo apt install ./SIGNA-1.6.1-linux-x64.deb` en la carpeta de descarga; cambia a ARM64 si corresponde. Apt resuelve las dependencias. Para actualizar, descarga el nuevo .deb y repite el comando; para desinstalar, `sudo apt remove signa`. Esta edición no reemplaza sus archivos mediante Velopack porque pertenecen a apt. Ubuntu también utiliza el formato .deb: su presencia no implica soporte Debian.

Desde 1.6.1 se retira el soporte de las demás distribuciones Linux: no se generan RPM ni paquetes Arch/Omarchy. Las descargas históricas se conservan. Esta decisión no elimina instalaciones ni datos anteriores.

## Datos, firma e integridad

Instalar, actualizar o desinstalar conserva la ubicación histórica de SQLite y los respaldos personales. No guardes bases ni respaldos dentro de la carpeta del programa. La base activa no está cifrada; los respaldos `.aulabk` sí, y su contraseña es necesaria para restaurarlos.

Windows continúa sin firma oficial y macOS sin Apple Developer ID/notarización. Pueden aparecer avisos de seguridad; descarga únicamente desde el repositorio oficial y comprueba los hashes. La firma ad hoc de macOS comprueba integridad, no identidad del desarrollador. Consulta el [procedimiento de Apple](https://support.apple.com/es-es/102445) si necesitas autorizar una app verificada; no desactives globalmente las protecciones del equipo.

`SHA256SUMS.txt` contiene los hashes de las siete distribuciones y los diez archivos de actualización. En Windows utiliza `Get-FileHash .\archivo.exe -Algorithm SHA256`; en Ubuntu/macOS, `sha256sum` o `shasum -a 256`. Los archivos `releases.<canal>.json` y `.nupkg` son utilizados por el actualizador: no debes abrirlos ni instalarlos manualmente.
