**SIGNA 1.6.1 — Acceso claro a actualizaciones y soporte Windows, macOS y Ubuntu**

### Mejoras

- **Buscar actualizaciones** ahora tiene icono vectorial, fondo, borde y estados visibles al pasar el cursor, pulsar y navegar con teclado. Se adapta a los colores personalizados y conserva el flujo de actualización existente.
- Versión de parche **1.6.1**: mejora compatible sobre 1.6.0, sin cambios en reglas académicas, cálculos, SQLite, preferencias ni respaldos.
- Soporte actual: **Windows 10/11 x64**, **macOS 15 o posterior Intel/Apple Silicon** y **Ubuntu 24.04 LTS x64/ARM64**.
- En Ubuntu se conservan **AppImage actualizable** y **.deb para apt**. Se retiran los paquetes RPM y Arch/Omarchy de las nuevas publicaciones; ya no se ofrece soporte Debian ni otras distribuciones. Ubuntu también usa .deb, por lo que ese formato se mantiene. Las Releases históricas permanecen intactas.
- Manifiesto y pipeline ajustados a siete distribuciones y cinco canales Velopack, con comprobaciones de integridad y rechazo de paquetes retirados o archivos privados.

### Cómo actualizar

Si tienes **1.6.0 con Velopack**, abre **Buscar actualizaciones**, revisa la versión y pulsa **Actualizar SIGNA**. Guarda antes tus cambios, cierra otras instancias y conserva un respaldo local cifrado fuera de la carpeta del programa. La actualización verifica el paquete y los datos antes de instalar y reiniciar; sin internet puedes continuar trabajando.

Las ediciones hasta 1.5.0 necesitan instalar una vez la edición actualizable desde esta Release. En Ubuntu, la actualización integrada corresponde a AppImage; el paquete .deb se actualiza descargando la nueva edición y utilizando apt. No abras dos ediciones a la vez.

En Windows se mantiene el asistente de **licencia MIT y selección de carpeta**, con Velopack como único gestor de actualizaciones/desinstalación. Los datos académicos mantienen su ubicación fuera de los archivos del programa.

Un commit en el repositorio no activa una actualización: la Release estable debe incluir su feed y paquete completo. Los cinco canales conservan sus nombres para poder actualizar desde 1.6.0.

### Descargas

- Windows 10/11 x64: `SIGNA-1.6.1-windows-x64-setup.exe`.
- Ubuntu 24.04 LTS x64/ARM64: `SIGNA-1.6.1-linux-x64.AppImage`, `SIGNA-1.6.1-linux-arm64.AppImage` y los paquetes `.deb` de ambas arquitecturas.
- macOS 15+: `SIGNA-1.6.1-osx-x64.dmg` (Intel) y `SIGNA-1.6.1-osx-arm64.dmg` (Apple Silicon).
- Cinco feeds `releases.<canal>.json` y cinco paquetes `-full.nupkg` para el actualizador. No son instaladores para abrir manualmente.

Incluyen .NET y el icono oficial. Consulta **INSTALACION.md**, **SHA256SUMS.txt** y [la guía de actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).

### Validación y limitaciones

El workflow compila y ejecuta las suites en cinco sistemas/arquitecturas, verifica los paquetes y sus hashes antes de marcar esta Release como estable. Comprueba instalación, reparación y desinstalación en Windows, instalación/desinstalación en Ubuntu y bundles/DMG en macOS, utilizando datos ficticios en entornos efímeros. La interfaz se revisa mediante capturas Avalonia Headless en tres tamaños y colores. Las pruebas manuales y el ciclo real de actualización/reinicio entre dos versiones instaladas corresponden al docente.

**Resultado de la publicación:** 429 pruebas de la aplicación aprobadas en cada uno de los cinco sistemas/arquitecturas, diez pruebas de empaquetado y comprobaciones nativas correctas. Los 19 adjuntos y sus 17 hashes se verificaron después de subirlos. [Ejecución completa](https://github.com/danielpose1996-stack/SIGNA/actions/runs/39722533263).

No se incluyen materias, estudiantes ni bases de prueba. Continúa sin firma oficial de Windows ni Apple Developer ID/notarización; macOS conserva firma ad hoc de integridad. Licencia MIT.

[Repositorio](https://github.com/danielpose1996-stack/SIGNA) · [Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md).
