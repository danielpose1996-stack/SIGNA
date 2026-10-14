# Instalar SIGNA

Descarga el archivo de tu sistema desde https://github.com/danielpose1996-stack/SIGNA/releases. Los paquetes incluyen .NET: no se necesita instalar el SDK. Conserva una copia cifrada de tus datos antes de actualizar.

| Equipo | Archivo |
| --- | --- |
| Windows 10/11 x64 | `SIGNA-1.1.0-windows-x64-setup.exe` |
| Ubuntu/Debian x64 | `SIGNA-1.1.0-linux-x64.deb` |
| Ubuntu/Debian ARM64 | `SIGNA-1.1.0-linux-arm64.deb` |
| Fedora y derivados x64 | `SIGNA-1.1.0-linux-x64.rpm` |
| Fedora y derivados ARM64 | `SIGNA-1.1.0-linux-arm64.rpm` |
| Mac Intel | `SIGNA-1.1.0-osx-x64.dmg` |
| Mac Apple Silicon (M1/M2/M3/etc.) | `SIGNA-1.1.0-osx-arm64.dmg` |

**Windows:** abre el instalador. Instala SIGNA para tu usuario, crea la entrada del menú Inicio y ofrece acceso directo al escritorio. No solicita administrador. Para desinstalar usa Aplicaciones instaladas; los datos se conservan.

**Ubuntu/Debian:** en la carpeta de descarga ejecuta `sudo apt install ./SIGNA-1.1.0-linux-x64.deb` (cambia a ARM64 si corresponde). Apt resuelve las bibliotecas del sistema. Para retirar el programa: `sudo apt remove signa`.

**Fedora:** ejecuta `sudo dnf install ./SIGNA-1.1.0-linux-x64.rpm` (cambia a ARM64 si corresponde). Para retirarlo: `sudo dnf remove signa`. Usa una distribución compatible con .NET 10 y X11/XWayland. Los paquetes no borran archivos de la carpeta del usuario.

**macOS:** abre el DMG de tu arquitectura y arrastra SIGNA a Applications. Expulsa el DMG y abre la aplicación desde Aplicaciones. Eliminar SIGNA.app conserva la base de datos local.

## Firma y seguridad

Esta distribución no tiene firma oficial de Windows ni certificado Apple Developer ID/notarización. Windows SmartScreen o macOS Gatekeeper pueden mostrar advertencias. Descarga desde el repositorio oficial y comprueba los hashes. En Mac se incluye firma ad hoc de integridad para el código nativo; no acredita la identidad del desarrollador. Consulta el [procedimiento oficial de Apple](https://support.apple.com/es-es/102445) si necesitas autorizar una app que has verificado. No desactives globalmente las protecciones del equipo.

`SHA256SUMS.txt` contiene los hashes de los siete instaladores. En Windows puedes calcular uno con `Get-FileHash .\archivo.exe -Algorithm SHA256`; en Linux/macOS utiliza `sha256sum` o `shasum -a 256`. Compara el resultado con ese archivo.

La base SQLite activa no está cifrada. Los respaldos `.aulabk` sí: guarda su contraseña para restaurarlos. Instalar, actualizar o desinstalar el programa no sustituye hacer respaldos.
