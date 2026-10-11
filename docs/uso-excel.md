# Importar estudiantes desde Excel

## Ejecutar

Abrir `Iniciar.cmd` con .NET 10 instalado o el SDK portable ya presente en `.tools/dotnet`. La aplicación abre sin inicio de sesión. Datos productivos: carpeta `AulaRegistro` en LocalApplicationData del usuario. `Iniciar-demo.cmd` abre el almacenamiento ficticio separado.

El iniciador regenera la compilación antes de abrir para incluir las vistas Avalonia y evitar reutilizar una compilación incompleta. Esperar el mensaje de compilación correcta; cerrar la aplicación antes de iniciar otra compilación. Si hay un error, queda visible en consola y no se ejecuta una versión anterior.

## Importar alumnos

1. Crear/seleccionar semestre, crear y abrir materia.
2. Pulsar **Importar Excel** en el encabezado de la materia, la pestaña **Estudiantes** o el menú **Estudiantes** con una materia abierta.
3. Elegir `.xlsx`. La aplicación detecta las columnas y muestra toda la lista automáticamente, incluso en listados sin encabezados.
4. Revisar nombre y documento y pulsar **Importar todos**. Todos los estudiantes nuevos válidos se guardan juntos en la materia y aparecen en Notas y Asistencias. No es necesario seleccionar cada estudiante.
5. Solo si la sugerencia no corresponde al archivo, abrir **Corregir columnas** y elegir documento, columnas del nombre y primera fila de estudiantes. Esta configuración se aplica a toda la lista y actualiza la vista previa automáticamente.

La detección de columnas es una sugerencia revisable. Documentos duplicados, existentes, incompletos o derivados de fórmulas no se importan. No se modifica el nombre de un estudiante existente con el mismo documento. Para elegir otro registro de un documento repetido, corregir una copia del Excel y volver a abrirla. Cancelar no modifica la materia. Si falla SQLite, no queda una importación parcial. Notas y asistencias previas conservan sus identificadores.

Un documento almacenado como texto conserva sus ceros iniciales. Se admite también formato numérico simple `000000`; si el archivo ya perdió los ceros o precisión numérica, el programa no puede reconstruirlos. Usar documentos como texto en el archivo original.

Límites: .xlsx convencional sin cifrar, 20 MiB de archivo, 80 MiB expandidos, 10000 filas por hoja, 256 columnas, 250000 celdas por libro y 5000 alumnos por materia. .xls y .xlsm todavía no están implementados. Fórmulas/macros no se ejecutan.

El Excel se usa como entrada de la lista de estudiantes. No hay un botón de exportación Excel en este flujo. El PDF individual del panel de asistencia sigue pendiente de especificación.

## Validación de esta entrega

68 pruebas correctas: 13 Domain, 23 Application, 15 Infrastructure y 17 Acceptance. Cubre detección con/sin encabezados sin perder el primer estudiante, importación de 40 alumnos sin selección individual, cambios de columnas que actualizan automáticamente la vista previa, importación atómica/aislada/reapertura SQLite, duplicados, hojas/textos compartidos/formatos de ceros, fórmulas y encabezados de dos líneas al 125 %.

Corrección de arranque: reproducido `No precompiled XAML found for GestorDocente.Desktop.App`; regeneración completa con 0 errores y 0 advertencias. Proceso real comprobado con ventana principal y estado Responding=True usando almacenamiento de prueba separado, sin inspeccionar ni controlar la interfaz del docente. La revisión visual sigue siendo manual.

Reportes internos de prueba leídos y renderizados con `@oai/artifact-tool`, herramienta independiente del lector de la aplicación. Evidencias ficticias en `artifacts/evidencias/011/reporte-notas.png` y `reporte-asistencias.png`; no representan el flujo solicitado de importación. No se usaron listas reales. No se pudo observar la nueva ventana de importación antes de que se cerrara. Por indicación del docente, la revisión visual la realizará manualmente; no se afirma una comprobación visual completada.

Skills utilizadas: Spreadsheets para lectura/render independiente; Computer Use para revisión nativa cuando disponible. No se añadieron dependencias NuGet ni skills. Referencia técnica: [estructura SpreadsheetML](https://learn.microsoft.com/en-us/office/open-xml/spreadsheet/structure-of-a-spreadsheetml-document), [celdas y cadenas en línea](https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.spreadsheet.cell).
