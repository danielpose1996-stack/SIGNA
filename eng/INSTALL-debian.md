# Instalar SIGNA 1.6.5 en Debian 13

Compatible con **Debian 13 (Trixie), x64/amd64 y ARM64**, con escritorio X11 o XWayland. Se agregan dos instaladores a la **misma Release v1.6.5**, sin cambiar versión ni reemplazar los archivos existentes de Windows, macOS o Ubuntu. No incluye Debian 12.

## Descargar

Desde [SIGNA 1.6.5](https://github.com/danielpose1996-stack/SIGNA/releases/tag/v1.6.5), descarga:

| Resultado de `dpkg --print-architecture` | Archivo |
| --- | --- |
| `amd64` | `SIGNA-1.6.5-debian13-x64.deb` |
| `arm64` | `SIGNA-1.6.5-debian13-arm64.deb` |

Descarga también **SHA256SUMS-debian13.txt** y compara el hash de tu archivo con `sha256sum nombre-del-archivo.deb`. El manifiesto original SHA256SUMS.txt corresponde a los archivos originales; se conserva sin modificaciones.

## Instalar

1. Abre una terminal en la carpeta donde descargaste el archivo.
2. Para x64, ejecuta:

   ```sh
   sudo apt update
   sudo apt install ./SIGNA-1.6.5-debian13-x64.deb
   ```

   Para ARM64 utiliza `SIGNA-1.6.5-debian13-arm64.deb`.

3. Abre **SIGNA** desde el menú de aplicaciones o ejecuta `signa`. Incluye .NET, el icono oficial y el acceso del menú; no necesitas instalar el SDK.

Apt instala las bibliotecas de Debian necesarias, incluidas `libicu76`, `libssl3t64`, `libc6` y las dependencias X11. No descargues ICU de Ubuntu ni mezcles repositorios. En sesiones Wayland debe estar disponible XWayland (`sudo apt install xwayland`).

## Actualizar o desinstalar

Para actualizar, cierra SIGNA, descarga el nuevo DEB de Debian y repite `sudo apt install ./...` con el nombre nuevo. No se configura un repositorio apt de SIGNA: `apt upgrade` por sí solo no descarga Releases de GitHub. El DEB no reemplaza sus archivos mediante Velopack.

Para desinstalar utiliza `sudo apt remove signa`. La base y los respaldos del docente permanecen fuera de `/usr/lib/signa`. Los instaladores Debian contienen exactamente los binarios de 1.6.5; únicamente cambia la dependencia ICU del paquete. No se han reemplazado feeds, paquetes de actualización ni el tag de esa versión.

## Datos y comprobaciones

Se mantiene la ubicación histórica de usuario: normalmente `~/.local/share/AulaRegistro/aula-registro.db`, o la ruta definida por `GESTOR_DOCENTE_DATA_DIR`. Para trasladar toda la información entre equipos usa **Respaldos**, no copies una base abierta.

Los instaladores se comprueban en contenedores oficiales Debian 13 de la misma arquitectura: instalación/desinstalación con apt, dependencias, icono, versión y arranque X11 mediante Xvfb con SQLite ficticio. Se verifica que el programa conserve los mismos binarios que la edición publicada. Estas pruebas no sustituyen la revisión manual del escritorio, los diálogos del sistema o el ciclo real de actualización.

Referencias: [.NET en Debian](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian), [dependencias Avalonia](https://docs.avaloniaui.net/docs/deployment/linux), [ICU de Debian 13](https://packages.debian.org/trixie/libicu76).
