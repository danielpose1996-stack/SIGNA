# 011 — Excel y encabezados legibles

Autorizado el 2026-10-11 por el pedido de importar nombre/documento de archivos .xlsx y corregir las tablas de Notas/Asistencias. Aclaración posterior del docente: Excel es el archivo de entrada de estudiantes; el botón principal debe importar, no exportar.

## Alcance y decisiones

- Importar .xlsx local, sin Excel instalado ni ejecución de fórmulas/macros. Detectar automáticamente hoja, encabezados y columnas de nombre/documento, incluso listados sin encabezados. Mostrar toda la lista de inmediato. Permitir combinar columnas de nombres/apellidos. Solo documento y nombre se guardan; documento es texto y conserva ceros iniciales cuando existen en la celda o su formato numérico simple.
- Aclaración de usabilidad autorizada por el docente: no seleccionar estudiantes uno por uno ni exigir pulsar Vista previa. Un botón **Importar todos** incorpora todos los registros nuevos válidos en una transacción. Documento identifica al estudiante dentro de la materia; duplicados/conflictos se muestran y no se sobrescriben. Filas incompletas o con fórmulas en campos seleccionados se rechazan. Cancelar no modifica nada; notas y asistencias existentes se conservan.
- La configuración de columnas es secundaria y plegable. Se aplica una sola vez a todo el archivo y actualiza automáticamente la lista. Si no se puede identificar el formato, mostrar indicaciones claras para elegir documento, nombre y primera fila de estudiantes. Para archivos sin encabezados se usa la estructura de los valores (nombre y documento contiguos); la vista previa permite revisar la sugerencia antes de guardar. Nunca perder el primer estudiante por tratarlo como encabezado. Preferir una hoja con listado frente a una portada vacía.
- Botón principal **Importar Excel** conectado al mismo flujo de importación de Estudiantes. El archivo original no se modifica. Se ignoran todos los datos distintos de nombre y documento; los nombres/apellidos separados pueden combinarse en un único nombre completo.
- La exportación Excel implementada antes de esta aclaración queda sin acceso en la interfaz y fuera del alcance solicitado. Se conservan los adaptadores internos existentes. PDF individual continúa pendiente de especificación.
- Corregir encabezados de ambas tablas con máximo dos líneas, altura suficiente, alineación y tooltip; nombres largos se abrevian visualmente sin cortar una línea por la mitad. Distinguir materia sin alumnos de filtro sin coincidencias. Conservar tabla predominante y detalle plegable.

## Implementación y límites

Contratos y proyecciones en Application; lector/escritor SpreadsheetML usando ZIP/XML incorporados en .NET en Infrastructure; MVVM y diálogos nativos de archivo en Desktop. No paquetes nuevos, Excel COM, red ni WebView. XML sin DTD/resolución externa; límites: archivo 20 MiB, ZIP expandido 80 MiB, XML individual 20 MiB, 10000 filas/hoja, 256 columnas, 250000 celdas/libro. Límite de 5000 alumnos por materia/importación.

Solo .xlsx convencional SpreadsheetML; .xls/.xlsm, libros cifrados y XML estricto no se anuncian como admitidos. La preselección de columnas es una ayuda revisable; no se importa sin vista previa.

## Aceptación y pruebas

XLS-01: hojas múltiples, columnas adicionales ignoradas, encabezado configurable y nombres compuestos.
XLS-02: ceros iniciales, texto compartido/en línea, filas dispersas, fórmulas/corruptos/límites, duplicados y conflictos visibles.
XLS-03: confirmar/cancelar/reimportar; aislamiento por materia y reapertura SQLite; fallo atómico sin pérdida de notas.
XLS-04: el botón principal abre la importación de estudiantes de la materia actual; únicamente se persisten documento y nombre, sin modificar el archivo fuente ni sobrescribir estudiantes existentes.
XLS-05: listado con encabezados o sin ellos y columnas adicionales muestra automáticamente todos los estudiantes, incluido el primero; un solo botón incorpora el lote completo de registros nuevos válidos.
XLS-06: cambiar hoja/columnas/primera fila actualiza la vista previa sin acciones adicionales; un formato no reconocible solicita corregir columnas, sin habilitar una importación vacía o inválida.
UI-01: encabezados completos a escala normal y 125 %, con lista larga, vista vacía y ventana reducida; validación headless y revisión nativa cuando disponible.
ARR-01 (corrección autorizada por «no me abre la aplicación»): Iniciar.cmd regenera la compilación, incluidas las vistas AXAML, antes de ejecutar sin recompilar. No reutilizar una DLL cuya compilación incremental haya quedado incompleta. Si falla la compilación, informar el error y no ejecutar una versión anterior. Validar que el proceso real crea una ventana y responde con datos de prueba separados; la revisión visual permanece manual por indicación del docente.

Archivos afectados: WorkspaceService, nuevos contratos/modelos de Excel, adaptadores de lectura/exportación, ViewModel/modal de importación, MainViewModel/MainWindow/App, TablePresentation, estilos y vistas académicas, pruebas y documentación. Los cambios pendientes de 009/010 se conservan y se vuelven a comprobar.
