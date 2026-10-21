# Historial de cambios de SIGNA

## 1.5.0 — 21 de octubre de 2026

- Copiar parámetros en las tarjetas de Materias y pegarlos opcionalmente al crear otra asignatura, incluso en otro semestre. Incluye cortes, porcentajes, actividades, subactividades y nota de asistencia habilitada, con resumen previo y configuración independiente. Excluye estudiantes, contactos, calificaciones, semanas y registros de asistencia; guardado atómico y sin modificar el origen.
- Selector HSV más fluido: cambios de saturación/brillo conjuntos, vista previa agrupada por intervalos, aplicación final al soltar y cancelación de cambios pendientes. Recursos agrupados y paleta Fluent reutilizada; dibujo del selector reutiliza pinceles y degradados.
- Contraste automático coherente en los fondos reales de botones primarios y cortes seleccionados, incluidos cursor encima, pulsación y deshabilitado. Bindings compilados en Personalización.
- Inicio con pantalla de carga e inicialización del almacenamiento fuera del hilo visual. Estadísticas progresivas con cancelación de lecturas obsoletas y reutilización por revisión de cada materia.
- Cambio de corte desde la interfaz asíncrono, búsqueda agrupada sin consultar SQLite por pulsación, modelos de celdas y detalles creados cuando se utilizan y mediciones de columnas compartidas entre tablas. Se conservan datos, cálculos, transacciones y esquema SQLite.

**Correcciones:** evita reaplicar el tema completo por cada evento del selector, corrige texto e iconos sin contraste sobre colores claros y reduce bloqueos del hilo visual al cargar datos. Omite consultas repetidas de estadísticas sin cambios, detiene cargas obsoletas y conserva el filtro y orden correctos al pegar calificaciones. No se alteran notas, reglas académicas, formato SQLite ni ubicación de datos.

**Actualización y descarga:** crea un respaldo local cifrado antes de actualizar. Los instaladores incluyen .NET y el icono oficial, conservan los datos existentes y no contienen bases de datos ni estudiantes de prueba. Descarga los siete instaladores de Windows, Linux y macOS desde [SIGNA 1.5.0](https://github.com/danielpose1996-stack/SIGNA/releases/tag/v1.5.0). Se distribuyen sin firma oficial de Windows ni Apple Developer ID/notarización. Las pruebas manuales quedan a cargo del docente.

## 1.4.0 — 20 de octubre de 2026

- **Registro masivo de estudiantes:** en Agregar estudiante, elige Varios estudiantes y pega un nombre completo por línea. También puedes pegar documentos, correos y celulares en sus propias columnas, en el mismo orden. Solo el nombre es obligatorio; conserva la opción individual.
- **Revisión antes de guardar:** vista previa completa, errores por fila, detección de documentos duplicados y comprobación de cantidades. Los campos opcionales sin dato mantienen su posición. El lote se guarda de manera atómica, sin sobrescribir alumnos existentes; los fallos conservan el borrador. Los celulares se ocultan inicialmente en la revisión.

- Barra de estudiantes renovada en la materia abierta: Agregar estudiante destacado, iconos vectoriales, búsqueda integrada, botones secundarios agrupados y etiquetas adaptables al espacio. Conserva las acciones existentes y el área de la tabla.
- Edición de actividades y subactividades con campos visibles, creación con foco automático, total del corte y errores cercanos. Las tarjetas de Notas abren la actividad elegida. Las semanas tienen acciones de editar/eliminar en cada fila y un formulario separado con fecha opcional y presencia masiva al crear. Se conservan las reglas y el guardado existentes.
- Porcentajes opcionales por subactividad, configurables de manera independiente para cada actividad principal. Total interno exacto de 100 %, promedio simple por defecto y conservación del peso principal dentro del corte.
- Actualización automática de resultados del corte, consolidada, métricas y reportes. Encabezados y PDF distinguen porcentajes internos y principales.
- Validación antes de guardar, ajuste explícito al retirar o recuperar subactividades y conservación de notas, vacíos y datos anteriores. Configuración incluida en respaldos locales cifrados.

**Actualización:** crea un respaldo local cifrado antes de instalar. Esta versión conserva los datos existentes y actualiza el formato académico para incluir las ponderaciones internas. Para volver a una versión anterior, utiliza un respaldo realizado antes de actualizar. Los instaladores no incluyen bases de datos ni listados de estudiantes.

Descarga los instaladores de Windows, Linux y macOS en [SIGNA 1.4.0](https://github.com/danielpose1996-stack/SIGNA/releases/tag/v1.4.0). Las pruebas manuales quedan a cargo del docente.

## 1.3.0 — 16 de octubre de 2026

### Nuevas funciones

- **Importación de Excel ampliada:** listados `.xls`, `.xlsx`, `.xlsm` y `.xlsb`, con detección de columnas, vista previa de todo el listado y validación de duplicados y datos. Se conservan nombre, documento y contactos disponibles. No requiere Excel instalado ni ejecuta macros o fórmulas.
- **Pegado masivo de calificaciones:** botón **Pegar notas** en cada actividad directa y subactividad. Acepta una nota por línea, de 0 a 5, con coma o punto decimal. La vista previa muestra los estudiantes en el orden visible de la tabla, incluido el filtro de búsqueda. La cantidad debe coincidir exactamente; las notas existentes requieren confirmar su reemplazo. Se guarda todo el lote en una sola operación.
- **Orden alfabético:** el encabezado **Nombre completo** alterna A–Z/Z–A en Notas y Asistencias. Cambia únicamente la presentación; conserva las relaciones de estudiantes con sus notas y asistencias.
- **Nota opcional de asistencia por corte:** configura libremente la valoración de P/A/J/T. La columna adicional calcula el promedio simple de semanas con estado asignado, incluida J; los pendientes y los registros sin clase se excluyen. Se puede desactivar y reactivar sin perder la configuración. Esta nota es independiente del porcentaje de asistencia y no interviene en las notas del corte ni en la consolidada.

### Correcciones y protección de datos

- Los archivos XLSX con celdas de texto compartido vacías se leen correctamente; ya no se confunden con libros dañados.
- La importación permite lectura compartida de archivos abiertos por otro programa cuando este admite leerlos. Los bloqueos exclusivos y los problemas de permisos muestran indicaciones claras.
- Se verifican las longitudes de los registros XLSB antes de leerlos y se rechazan encabezados truncados o libros sin hojas legibles. Se mantienen límites de tamaño y contenido.
- El pegado valida el lote completo, conserva el borrador ante fallos y rechaza guardados sobre una materia modificada por otra instancia, evitando asignaciones parciales.
- La configuración de asistencia se conserva al reiniciar y en los respaldos locales cifrados. La actualización mantiene las materias, estudiantes, notas, porcentajes y asistencias existentes.

**Antes de actualizar:** crea un respaldo local cifrado y conserva su contraseña. Para volver a una versión anterior, utiliza una copia realizada antes de esta actualización; las versiones anteriores no reconocen el nuevo formato de configuración académica.

Los instaladores se descargan desde [SIGNA 1.3.0](https://github.com/danielpose1996-stack/SIGNA/releases/tag/v1.3.0). Incluyen .NET y se distribuyen sin firma oficial de Windows ni Apple Developer ID/notarización. Los datos reales del docente no forman parte de los instaladores.

## 1.2.0

- Reporte PDF de notas por corte para toda la materia, identificado únicamente con nombres de estudiantes.
- Selector de color HSV y código HEX sincronizados, vista previa y conservación del color guardado; se retiraron las paletas predeterminadas.

## 1.1.0

- Edición de semanas de asistencia y numeración visible consecutiva sin reasignar registros.
- Nombre docente, personalización del perfil y colores opcionales de rendimiento académico.
