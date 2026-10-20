**SIGNA 1.4.0 — Sistema Integral de Gestión de Notas y Asistencias**.

### Novedades de esta actualización

- **Agregar varios estudiantes de una sola vez:** abre **Agregar estudiante → Varios estudiantes** y pega un nombre completo por línea desde Word, Bloc de notas u otra fuente. El formulario individual sigue disponible.
- **Documentos y contactos en lote:** también puedes pegar las listas de documentos, correos y celulares en sus propias columnas, respetando el orden de los nombres. Solo el nombre es obligatorio. Las columnas opcionales utilizadas deben tener la misma cantidad de filas; deja líneas vacías donde falten datos. Los documentos se conservan como texto, incluidos los ceros iniciales.
- **Vista previa obligatoria:** pulsa **Revisar lista**, verifica los datos y corrige los errores antes de guardar. Se detectan documentos repetidos o ya registrados, nombres faltantes, correos inválidos y cantidades desiguales. Los celulares aparecen ocultos inicialmente en la revisión, con opción de mostrarlos. El lote completo se guarda en una sola operación, sin reemplazar alumnos existentes. Cancelar no guarda y los errores conservan el borrador.
- **Porcentajes opcionales de subactividades:** cada actividad principal puede mantener el promedio simple o usar porcentajes internos que sumen exactamente 100 %. Por ejemplo, Teoría 40 % y Práctica 60 %, sin alterar el porcentaje del parcial dentro del corte. Se actualizan automáticamente las definitivas, la nota consolidada, las métricas y los reportes PDF. Los vacíos siguen contando como cero únicamente en el cálculo.
- **Gestión de evaluaciones más sencilla:** campos de edición visibles, foco automático en las nuevas actividades/subactividades, totales y validaciones cercanas. Las tarjetas de Notas abren directamente la actividad seleccionada y las eliminaciones siguen requiriendo confirmación.
- **Semanas de asistencia más prácticas:** acciones Editar y Eliminar junto a cada semana y formulario separado, conservando fecha opcional y Marcar presente a todos al crear.
- **Barra de estudiantes renovada:** Agregar estudiante destacado, búsqueda integrada, iconos consistentes y etiquetas adaptables al espacio, conservando el área de las tablas.

### Integridad de la información

- Las líneas vacías de los datos opcionales conservan su posición, para evitar asignar un contacto al estudiante equivocado.
- Una lista modificada debe revisarse otra vez. Si la materia cambia desde la revisión, se rechaza el guardado y se solicita actualizarla.
- Los conflictos entre instancias y fallos de escritura no dejan registros parciales. El registro masivo usa el almacenamiento transaccional existente y conserva notas, asistencias y otras materias.
- Las ponderaciones internas se validan al guardar, retirar o recuperar subactividades, sin borrar calificaciones ni redistribuir porcentajes silenciosamente. Se incluyen en los respaldos locales cifrados.

Se conservan los PDF de notas y asistencia, el selector HSV/HEX, la gestión de materias y estudiantes, la nota consolidada y los respaldos locales cifrados. El [historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) describe las novedades en el repositorio.

### Actualizar conservando la información

Antes de instalar, crea un respaldo local cifrado y conserva su contraseña. Cierra SIGNA y utiliza el instalador de tu sistema. Se conserva la ubicación de datos y la información existente. El formato académico se actualiza para incluir las ponderaciones internas; para volver a una versión anterior, restaura una copia realizada antes de esta actualización. Los instaladores no contienen bases de datos, materias ni estudiantes de prueba.

### Descarga

- Windows 10/11 x64: `SIGNA-1.4.0-windows-x64-setup.exe`.
- Ubuntu/Debian x64 y ARM64: paquetes `.deb`.
- Fedora y derivados x64 y ARM64: paquetes `.rpm`.
- macOS 15 o posterior: `.dmg` para Intel (`osx-x64`) y Apple Silicon (`osx-arm64`).

Todos incluyen .NET y el icono oficial. Lee **INSTALACION.md** y comprueba **SHA256SUMS.txt**. Los paquetes de Linux declaran sus bibliotecas del sistema; se requiere X11 o XWayland. Los datos locales y respaldos no están incluidos en los instaladores y se conservan al desinstalar.

### Validación y firma

Compilaciones y pruebas automatizadas en runners nativos de Windows, Linux x64/ARM64 y macOS Intel/Apple Silicon. Se verifican arquitectura, recursos, licencias, instalación/desinstalación aislada de Windows y DEB, contenido RPM e integridad/montaje de DMG. Avalonia se prueba mediante Headless; las pruebas manuales quedan a cargo del docente.

**Sin firma oficial de Windows ni Apple Developer ID/notarización.** Windows/macOS pueden mostrar advertencias. La firma ad hoc de macOS valida el código nativo, pero no identifica al desarrollador. No se solicita desactivar globalmente la seguridad del sistema.

Licencia MIT. Repositorio: https://github.com/danielpose1996-stack/SIGNA
