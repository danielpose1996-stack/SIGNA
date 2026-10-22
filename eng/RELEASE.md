**SIGNA 1.6.0 — Actualizaciones integradas**

### Novedades

- **Instalador Windows revisado, sin cambiar 1.6.0:** ahora muestra la licencia MIT y permite seleccionar la carpeta. Conserva Velopack como gestor de actualizaciones/desinstalación y rechaza carpetas con datos o archivos ajenos. Si ya descargaste el EXE anterior, descárgalo nuevamente para obtener el asistente nuevo. Se conserva el tag, los cinco paquetes/feeds de actualización y los instaladores de macOS/Linux; se actualiza únicamente el EXE de Windows y su hash, además de las instrucciones.

- **Buscar actualizaciones** en el menú lateral y comprobación de versiones estables al iniciar, en segundo plano. Sin conexión puedes seguir trabajando normalmente.
- Aviso con versión actual y disponible. **Actualizar SIGNA** descarga, verifica, instala y reinicia mediante Velopack, con progreso, cancelación antes del reinicio y mensajes para reintentar ante errores.
- Se rechazan versiones preliminares, downgrades y candidatos distintos del revisado. Se comprueba tamaño/SHA-256 tras descargar y antes del reinicio, también para archivos ya existentes en caché.
- Protección ante cambios sin guardar, operaciones activas, otras instancias, SQLite inválido y datos o respaldos dentro de la carpeta reemplazable. Conserva la ubicación, el esquema y los registros de SQLite, las preferencias y los respaldos cifrados.
- Instalador Windows preparado con Velopack, DMG macOS con bundle actualizable y **AppImage Linux x64/ARM64**. Se mantienen los paquetes DEB, RPM y Arch/Omarchy.

### Transición desde versiones anteriores

Las ediciones hasta 1.5.0 no tienen este actualizador: instala **una vez** la nueva edición para poder recibir actualizaciones internas posteriores. Crea antes un respaldo local cifrado, conserva su contraseña y cierra SIGNA. En Windows no es necesario desinstalar primero la edición anterior; utiliza el acceso nuevo y evita abrir ambas ediciones a la vez. En macOS reemplaza SIGNA.app desde el DMG y ejecútala desde su ubicación definitiva. En Linux utiliza AppImage para la actualización integrada; DEB/RPM/Arch se administran con apt/dnf/pacman.

No se incluyen bases, estudiantes ni materias de prueba en los instaladores. No hay cambios de reglas académicas ni de esquema SQLite. Los respaldos deben guardarse fuera de la carpeta del programa.

### Archivos

- Windows 10/11 x64: `SIGNA-1.6.0-windows-x64-setup.exe`.
- Linux x64/ARM64: `SIGNA-1.6.0-linux-x64.AppImage` y `SIGNA-1.6.0-linux-arm64.AppImage`, más los DEB/RPM de ambas arquitecturas.
- Arch Linux/Omarchy x64: `SIGNA-1.6.0-archlinux-x64.pkg.tar.zst`.
- macOS 15 o posterior: `SIGNA-1.6.0-osx-x64.dmg` (Intel) y `SIGNA-1.6.0-osx-arm64.dmg` (Apple Silicon).
- Los cinco archivos `releases.<canal>.json` y sus paquetes `-full.nupkg` son utilizados por el actualizador; no son instaladores para abrir manualmente.

Incluyen .NET y el icono oficial. Consulta **INSTALACION.md**, **SHA256SUMS.txt** y [la guía de actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).

### Validación y limitaciones

El workflow compila y prueba en cada sistema nativo, comprueba las distribuciones y exige los cinco feeds completos con canal, versión, identidad y SHA-256 correctos antes de publicar. Las pruebas incluyen estados del actualizador, fallos/cancelación, datos SQLite ficticios y WAL, caché corrupta y presentación Avalonia Headless. Las pruebas manuales, incluida la transición y el ciclo de instalación/reinicio entre versiones instaladas, corresponden al docente.

**Validación de esta publicación:** 427 pruebas aprobadas en cada uno de los cinco sistemas/arquitecturas (Windows x64, Linux x64/ARM64 y macOS Intel/Apple Silicon), seis pruebas de empaquetado y comprobaciones nativas de las distribuciones. Incluye instalación/desinstalación en Windows y Arch con conservación de datos ficticios, verificación de bundles/DMG y AppImage, y descarga de los archivos subidos para comprobar sus SHA-256 antes de marcar la Release como estable.

**Validación del instalador Windows revisado:** compilación correcta, 427 pruebas de la aplicación y nueve pruebas de empaquetado aprobadas. En un equipo efímero de GitHub Actions se verificaron instalación en una carpeta elegida con espacios y caracteres Unicode, reparación, desinstalación y rechazo de destinos inseguros o con datos, conservando los archivos ficticios. La revisión visual del asistente queda a cargo del docente.

Continúa **sin firma oficial de Windows ni Apple Developer ID/notarización**, según las distribuciones actuales. La firma ad hoc de macOS verifica integridad, no identidad del desarrollador. Pueden aparecer advertencias del sistema; no desactives globalmente sus protecciones. El paquete Arch también se distribuye sin firma oficial de repositorio.

Licencia MIT. [Repositorio](https://github.com/danielpose1996-stack/SIGNA) · [Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md).
