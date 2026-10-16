# Historial de cambios de SIGNA

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
