**SIGNA 1.3.0 — Sistema Integral de Gestión de Notas y Asistencias**.

### Novedades de esta actualización

- **Más formatos de Excel:** importa listados `.xls`, `.xlsx`, `.xlsm` y `.xlsb`. Conserva la detección de columnas, vista previa, revisión de duplicados y contactos opcionales. No requiere Excel instalado ni ejecuta macros o fórmulas; en XLS/XLSB se leen los valores guardados del libro.
- **Pegar calificaciones de una sola vez:** nuevo botón **Pegar notas** en el encabezado de cada actividad directa o subactividad. Pega una columna con una nota por línea, de 0 a 5, usando coma o punto decimal.
- **Vista previa para evitar asignaciones incorrectas:** las notas se muestran junto a los nombres, respetando exactamente el orden visible de la tabla y sus filtros. La cantidad debe coincidir; si ya existen notas, debes confirmar el reemplazo. Todo el lote se guarda en una sola operación y los resultados se actualizan automáticamente.
- **Ordenar por nombre:** pulsa **Nombre completo** para alternar A–Z/Z–A en Notas y Asistencias, sin modificar los datos almacenados ni sus relaciones.
- **Nota de asistencia opcional:** activa la opción por corte y configura libremente las valoraciones de Presente, Ausente, Justificada y Tarde. Se promedian únicamente las semanas con un estado asignado. Puedes ocultar la columna sin perder los valores configurados. Esta nota es informativa e independiente del porcentaje de asistencia y de las calificaciones académicas.

### Errores corregidos

- Lectura de XLSX con celdas de texto vacías: ya no se muestran como archivos dañados.
- Apertura de archivos que están abiertos en otro programa cuando permiten lectura compartida; avisos claros ante bloqueo exclusivo o falta de permisos.
- Validación de registros binarios XLSB y archivos truncados, sin reservas de memoria desproporcionadas ni aceptación de libros inválidos como vacíos.
- Guardado de notas en lote con validación completa, control de concurrencia y conservación del borrador si falla la escritura.

Se conservan los PDF de notas y asistencia, el selector HSV/HEX, la gestión de materias y estudiantes, la nota consolidada y los respaldos locales cifrados. El [historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) describe las novedades en el repositorio.

### Actualizar conservando la información

Antes de instalar, crea un respaldo local cifrado y conserva su contraseña. Cierra SIGNA y utiliza el instalador de tu sistema. Se conserva la ubicación de datos y la información existente. La nueva configuración académica se incluye en los respaldos; para volver a una versión anterior, restaura una copia realizada antes de esta actualización. Los instaladores no contienen bases de datos, materias ni estudiantes de prueba.

### Descarga

- Windows 10/11 x64: `SIGNA-1.3.0-windows-x64-setup.exe`.
- Ubuntu/Debian x64 y ARM64: paquetes `.deb`.
- Fedora y derivados x64 y ARM64: paquetes `.rpm`.
- macOS 15 o posterior: `.dmg` para Intel (`osx-x64`) y Apple Silicon (`osx-arm64`).

Todos incluyen .NET y el icono oficial. Lee **INSTALACION.md** y comprueba **SHA256SUMS.txt**. Los paquetes de Linux declaran sus bibliotecas del sistema; se requiere X11 o XWayland. Los datos locales y respaldos no están incluidos en los instaladores y se conservan al desinstalar.

### Validación y firma

Compilaciones y pruebas automatizadas en runners nativos de Windows, Linux x64/ARM64 y macOS Intel/Apple Silicon. Se verifican arquitectura, recursos, licencias, instalación/desinstalación aislada de Windows y DEB, contenido RPM e integridad/montaje de DMG. Avalonia se prueba mediante Headless; las pruebas manuales quedan a cargo del docente.

**Sin firma oficial de Windows ni Apple Developer ID/notarización.** Windows/macOS pueden mostrar advertencias. La firma ad hoc de macOS valida el código nativo, pero no identifica al desarrollador. No se solicita desactivar globalmente la seguridad del sistema.

Licencia MIT. Repositorio: https://github.com/danielpose1996-stack/SIGNA
