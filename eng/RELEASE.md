**SIGNA 1.6.3 — Reemplaza una lista de estudiantes sin perder la configuración de la materia**

### Novedades

- **Eliminar lista** en las barras de **Notas y Asistencias**, con papelera, borde rojo suave y texto visible. En Notas aparece antes de Agregar estudiante, en la zona de acciones del listado.
- **Confirmación clara:** muestra la asignatura, código, período, grupo y cantidad de estudiantes, notas y registros de asistencia afectados. Cancelar, cerrar el diálogo o pulsar Escape no borra nada; el foco inicial está en Cancelar.
- **Borrado completo por materia:** al confirmar se retiran todos sus estudiantes y sus registros de notas/asistencias en todos los cortes, incluidos registros de actividades o semanas retiradas. El filtro de búsqueda no limita esta acción.
- **Configuración intacta:** conserva materia, semestre, cortes, ponderaciones, actividades, subactividades, semanas y fechas, configuración de notas por asistencia e historial de configuración. No cambia las demás materias ni el perfil del docente.
- **Un único guardado:** elimina la lista de forma atómica con control de concurrencia. Un fallo de escritura o una confirmación obsoleta no deja borrados parciales. Las notas o asistencias pendientes deben guardarse o corregirse antes de eliminar la lista.
- **Preparada para un listado nuevo:** se actualizan las tablas, selección, resúmenes, conteos y Nota consolidada; se limpia la búsqueda y siguen disponibles Importar Excel y Agregar estudiante. El botón se deshabilita cuando no hay alumnos o hay una operación en curso.
- **Barra adaptable:** conserva los accesos existentes en ventanas pequeñas y escala 125 %, sin recortar el nuevo botón. No modifica las reglas académicas, el esquema de SQLite, los respaldos ni las actualizaciones.

### Cómo usarlo

1. Abre la materia correcta y entra en Notas o Asistencias.
2. Si necesitas conservar la lista anterior, genera un respaldo antes de borrarla.
3. Pulsa **Eliminar lista**. Revisa la materia y los conteos del diálogo.
4. Confirma solo si quieres eliminar **la lista completa y sus notas/asistencias de todos los cortes**. Para recuperar lo borrado necesitarás restaurar un respaldo anterior.
5. Importa el Excel correcto o agrega los nuevos estudiantes. La estructura de evaluación y las semanas de la materia siguen disponibles.

Actualizar SIGNA no elimina estudiantes automáticamente. La función requiere una acción explícita y su confirmación.

### Actualización y descargas

Desde **SIGNA 1.6.0–1.6.2 con Velopack**, utiliza **Buscar actualizaciones → Actualizar SIGNA**. Se conservan los datos, preferencias, respaldos y configuración de la carpeta predeterminada. Sin internet puedes seguir trabajando. En Ubuntu, la actualización interna corresponde a AppImage; el paquete .deb se actualiza con apt. Las ediciones hasta 1.5.0 necesitan instalar una vez la edición preparada con Velopack.

- **Windows 10/11 x64:** `SIGNA-1.6.3-windows-x64-setup.exe`, asistente con licencia MIT y selección de carpeta.
- **Ubuntu 24.04 LTS x64/ARM64:** `SIGNA-1.6.3-linux-x64.AppImage`, `SIGNA-1.6.3-linux-arm64.AppImage` y los paquetes `.deb` de ambas arquitecturas.
- **macOS 15+, Intel/Apple Silicon:** `SIGNA-1.6.3-osx-x64.dmg` y `SIGNA-1.6.3-osx-arm64.dmg`.
- Cinco feeds `releases.<canal>.json` y cinco paquetes completos `-full.nupkg` para el actualizador; no son instaladores para abrir manualmente.

Consulta **INSTALACION.md** y **SHA256SUMS.txt**. Incluye .NET y el icono oficial. Se conservan las publicaciones históricas; sin paquetes nuevos de Arch/Omarchy o RPM.

### Validación y límites

El pipeline compila y prueba en los cinco sistemas/arquitecturas antes de publicar, verifica los paquetes nativos y descarga los adjuntos para comprobar sus hashes antes de marcar la Release como estable. Las pruebas nuevas comprueban confirmación/cancelación, filtros, atomicidad, rollback, conflictos entre instancias, conservación de otra materia y de la configuración, importación posterior y portabilidad de respaldos. Las capturas Avalonia Headless utilizan únicamente datos ficticios. La comprobación manual final y el ciclo real de actualización corresponden al docente.

Los instaladores no incluyen bases, estudiantes ni materias de prueba. Windows sigue sin firma oficial y macOS sin Apple Developer ID/notarización, manteniendo firma ad hoc de integridad. Licencia MIT.

[Repositorio](https://github.com/danielpose1996-stack/SIGNA) · [Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) · [Guía de actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).
