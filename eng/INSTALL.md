# Instalar SIGNA

Instrucciones de la edición **1.6.0**. Las instalaciones hasta 1.5.0 requieren instalar una vez esta edición para activar Velopack; luego podrán actualizar desde SIGNA. Consulta [el procedimiento completo](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).

Descarga el archivo de tu sistema desde https://github.com/danielpose1996-stack/SIGNA/releases. Los paquetes incluyen .NET: no se necesita instalar el SDK. Conserva una copia cifrada de tus datos antes de actualizar.

| Equipo | Archivo |
| --- | --- |
| Windows 10/11 x64 | `SIGNA-1.6.0-windows-x64-setup.exe` |
| Linux x64 o ARM64, actualización integrada | `SIGNA-1.6.0-linux-x64.AppImage` o `SIGNA-1.6.0-linux-arm64.AppImage` |
| Ubuntu/Debian x64 | `SIGNA-1.6.0-linux-x64.deb` |
| Ubuntu/Debian ARM64 | `SIGNA-1.6.0-linux-arm64.deb` |
| Fedora y derivados x64 | `SIGNA-1.6.0-linux-x64.rpm` |
| Fedora y derivados ARM64 | `SIGNA-1.6.0-linux-arm64.rpm` |
| Arch Linux / Omarchy x64 | `SIGNA-1.6.0-archlinux-x64.pkg.tar.zst` |
| Mac Intel | `SIGNA-1.6.0-osx-x64.dmg` |
| Mac Apple Silicon (M1/M2/M3/etc.) | `SIGNA-1.6.0-osx-arm64.dmg` |

**Windows:** abre el instalador, revisa la licencia MIT y elige la carpeta de instalación. La opción predeterminada es `%LOCALAPPDATA%\SIGNA`; puedes usar otra carpeta local donde tu cuenta tenga permisos de escritura. Elige una carpeta vacía o la instalación compatible de SIGNA que quieras reparar. El asistente rechaza carpetas con archivos ajenos, bases o respaldos para no reemplazar tus datos. Instala SIGNA para tu usuario, crea accesos en Inicio y el escritorio y conserva Velopack para actualizaciones/desinstalación. No solicita administrador. Para desinstalar usa Aplicaciones instaladas; los datos se conservan.

La revisión del instalador conserva **1.6.0** y el nombre del EXE. Si descargaste la primera edición de ese instalador y deseas el selector de carpeta, vuelve a descargarlo y comprueba el hash nuevo. Las actualizaciones integradas del programa se ofrecerán cuando se publique una versión superior; esta revisión del asistente no modifica los paquetes de actualización ni requiere reinstalar para seguir usando la aplicación.

**Ubuntu/Debian:** en la carpeta de descarga ejecuta `sudo apt install ./SIGNA-1.6.0-linux-x64.deb` (cambia a ARM64 si corresponde). Apt resuelve las bibliotecas del sistema. Para retirar el programa: `sudo apt remove signa`.

**Fedora:** ejecuta `sudo dnf install ./SIGNA-1.6.0-linux-x64.rpm` (cambia a ARM64 si corresponde). Para retirarlo: `sudo dnf remove signa`. Usa una distribución compatible con .NET 10 y X11/XWayland. Los paquetes no borran archivos de la carpeta del usuario.

**macOS:** abre el DMG de tu arquitectura y arrastra SIGNA a Applications. Expulsa el DMG y abre la aplicación desde Aplicaciones. Eliminar SIGNA.app conserva la base de datos local.

**Linux AppImage:** conserva la AppImage de tu arquitectura en una carpeta propia estable y escribible. Ejecuta `chmod +x SIGNA-1.6.0-linux-x64.AppImage` y ábrela (ajusta el nombre para ARM64). Requiere soporte AppImage/FUSE, las bibliotecas nativas de .NET/Avalonia y X11/XWayland. Esta edición admite **Buscar actualizaciones → Actualizar SIGNA**, igual que Windows y macOS. Los DEB/RPM/Arch se actualizan mediante su gestor; SIGNA no sustituye los archivos de `/usr`.

**Arch Linux / Omarchy (x86_64):** en la carpeta de descarga ejecuta `sudo pacman -U ./SIGNA-1.6.0-archlinux-x64.pkg.tar.zst`. Pacman instala el paquete local y resuelve sus dependencias desde los repositorios configurados. Incluye .NET; declara XWayland para el backend X11 de SIGNA en escritorios Wayland/Hyprland. Abre SIGNA desde el menú o ejecuta `signa`. Para retirarlo: `sudo pacman -R signa`; los datos y respaldos personales se conservan. Es un paquete binario local de esta Release, sin registro automático en AUR; las nuevas versiones se descargan e instalan de la misma manera. La prueba automatizada se realiza en Arch, sin revisar visualmente el escritorio Omarchy.

## Firma y seguridad

Esta distribución no tiene firma oficial de Windows ni certificado Apple Developer ID/notarización. Windows SmartScreen o macOS Gatekeeper pueden mostrar advertencias. Descarga desde el repositorio oficial y comprueba los hashes. En Mac se incluye firma ad hoc de integridad para el código nativo; no acredita la identidad del desarrollador. Consulta el [procedimiento oficial de Apple](https://support.apple.com/es-es/102445) si necesitas autorizar una app que has verificado. No desactives globalmente las protecciones del equipo.

`SHA256SUMS.txt` contiene los hashes de las distribuciones y los archivos de actualización. En Windows puedes calcular uno con `Get-FileHash .\archivo.exe -Algorithm SHA256`; en Linux/macOS utiliza `sha256sum` o `shasum -a 256`. Compara el resultado con ese archivo. El paquete Arch también se distribuye sin firma oficial; no desactives globalmente la verificación de firmas de Pacman.

La base SQLite activa no está cifrada. Los respaldos `.aulabk` sí: guarda su contraseña para restaurarlos. Instalar, actualizar o desinstalar el programa no sustituye hacer respaldos.
