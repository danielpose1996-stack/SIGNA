**SIGNA 1.2.0 — Sistema Integral de Gestión de Notas y Asistencias**.

### Novedades

- **PDF de notas por corte:** nuevo botón «Generar reporte PDF» en Notas. Incluye la asignatura, período, grupo, corte y todos los estudiantes, aunque exista una búsqueda activa.
- **Privacidad al compartir:** la única identificación de estudiantes en el reporte es su nombre completo. No se incluyen documentos de identidad, correos, teléfonos ni identificadores internos.
- **Tabla académica completa:** actividades, subactividades, porcentajes, promedios y definitiva. Las celdas pendientes siguen vacías y aportan cero únicamente a los cálculos. Tablas extensas divididas en secciones y páginas legibles, con encabezados, nombre y definitiva repetidos.
- **Selector HSV/HEX:** lienzo de saturación y brillo, deslizador de tono y campo hexadecimal sincronizado. Vista previa en tiempo real, accesible por teclado y adaptable al tamaño de la ventana.
- **Color único:** se retiran las paletas y el selector RGB anterior. El color ya guardado se conserva; «Guardar apariencia» confirma los cambios y «Cancelar» o cerrar revierte la vista previa. Se mantienen nombre docente, avatar y colores de rendimiento.

Se mantienen los reportes individuales de asistencia, las reglas académicas y los respaldos locales cifrados. La actualización conserva la ubicación de los datos. Antes de actualizar, crea un respaldo. Los instaladores no contienen materias ni estudiantes de prueba.

### Descarga

- Windows x64: instalador `windows-x64-setup.exe`.
- Ubuntu/Debian: `.deb` para x64 o ARM64.
- Fedora y derivados: `.rpm` para x64 o ARM64.
- macOS: `.dmg` separado para Intel (`osx-x64`) y Apple Silicon (`osx-arm64`).

Todos incluyen .NET y el icono oficial. Lee **INSTALACION.md** y comprueba **SHA256SUMS.txt**. Los paquetes de Linux declaran sus bibliotecas del sistema; se requiere X11 o XWayland. Los datos locales y respaldos no están incluidos en los instaladores y se conservan al desinstalar.

### Validación y firma

Compilaciones y pruebas automatizadas en runners nativos de Windows, Linux x64/ARM64 y macOS Intel/Apple Silicon. Se verifican las arquitecturas, recursos, licencias, instalación/desinstalación aislada de Windows y DEB, contenido RPM e integridad/montaje de DMG. Las pruebas Avalonia utilizan Headless; no se afirma revisión manual de todas las pantallas en estos equipos.

**Sin firma oficial de Windows ni Apple Developer ID/notarización.** Windows/macOS pueden mostrar advertencias. La firma ad hoc de macOS valida el código nativo, pero no identifica al desarrollador. No se solicita desactivar globalmente la seguridad del sistema.

Licencia MIT. Repositorio: https://github.com/danielpose1996-stack/SIGNA
