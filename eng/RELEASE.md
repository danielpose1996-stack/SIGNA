**SIGNA 1.6.5 — Asistencia consolidada y tarjetas de seguimiento en PDF**

### Novedades

- **Nueva pestaña Asistencia consolidada**, junto a Notas, Asistencias y Nota consolidada. Presenta a los estudiantes, una columna de ausencias (A) por cada corte y el **Total de faltas** de la materia. Respeta los nombres personalizados y los cortes adicionales.
- **Actualización automática:** al guardar una asistencia se actualiza el alumno afectado, incluso si se editó otro corte. También se ajusta al retirar o recuperar semanas, agregar/eliminar/renombrar cortes y modificar la lista de estudiantes.
- **Solo cuenta A:** se incluyen las semanas activas de clase y de actividades. Presente (P), Justificada (J), Tarde (T), sin clase, pendientes y semanas retiradas no suman faltas. Este resumen no modifica el porcentaje, la clasificación ni la nota opcional de asistencia.
- **PDF de Nota consolidada con tarjetas:** después de la tabla se agregan los alumnos Crítico y Delicado, críticos primero, con su nombre completo, correo cuando existe y nota mínima para aprobar. Conserva el diseño de las tarjetas actuales y distribuye las tarjetas entre páginas sin cortarlas.

### Datos incluidos en el PDF

La tabla de calificaciones mantiene únicamente los nombres como identificación. **El correo se incluye exclusivamente en las tarjetas de alumnos críticos y delicados**; no se exportan correos de otros estados. Si falta un correo aparece «Sin correo registrado». El PDF nunca incluye documentos, teléfonos ni identificadores internos. Si no hay alumnos en riesgo, no agrega una página vacía de tarjetas.

El botón de exportación y el mensaje al guardar reflejan esta ampliación. Se conserva el selector de destino y la confirmación antes de reemplazar archivos. No cambian las fórmulas, los porcentajes, la nota mínima ni los estados académicos existentes.

### Rendimiento y compatibilidad

- Conteos en memoria y actualización del alumno afectado; la búsqueda agrupada no genera consultas ni guardados adicionales en SQLite.
- Tabla con filas alternadas, encabezados y nombres consultables, virtualización y desplazamiento horizontal. Interfaz revisada con datos ficticios en ventanas normales, pequeñas y escala 125 %.
- Sin cambios de esquema, migraciones ni dependencias nuevas. Conserva el agregado académico 8, materias, estudiantes, notas, asistencias, preferencias y respaldos en sus ubicaciones habituales.

### Cómo utilizar las novedades

1. Abre una materia y entra en **Asistencia consolidada**. Consulta las ausencias por corte y el **Total de faltas**; desplaza horizontalmente la tabla si no caben todos los cortes.
2. Registra o corrige asistencias en **Asistencias**. Al guardarlas, el consolidado se actualiza automáticamente. Retirar una semana excluye sus registros del total; recuperarla vuelve a incluirlos.
3. En **Nota consolidada**, pulsa **Generar reporte PDF**, elige dónde guardarlo y revisa las tarjetas posteriores a la tabla. Estas tarjetas incluyen el correo de los alumnos en riesgo cuando está registrado.

### Actualización y descargas

Desde **SIGNA 1.6.0–1.6.4 con Velopack**, pulsa **Buscar actualizaciones → Actualizar SIGNA**. No necesitas desinstalar. Genera un respaldo local antes de actualizar; los datos y respaldos existentes se conservan. Sin internet puedes seguir trabajando.

- **Windows 10/11 x64:** `SIGNA-1.6.5-windows-x64-setup.exe`, con licencia MIT y selección de carpeta.
- **Ubuntu 24.04 LTS x64/ARM64:** `SIGNA-1.6.5-linux-x64.AppImage`, `SIGNA-1.6.5-linux-arm64.AppImage` y los paquetes `.deb` de ambas arquitecturas. La actualización integrada corresponde a AppImage; los paquetes `.deb` se actualizan instalando la nueva edición con apt.
- **macOS 15+, Intel/Apple Silicon:** `SIGNA-1.6.5-osx-x64.dmg` y `SIGNA-1.6.5-osx-arm64.dmg`.
- Los cinco feeds `releases.<canal>.json` y cinco archivos `-full.nupkg` son para el actualizador; no son instaladores para abrir manualmente.

Consulta **INSTALACION.md** y **SHA256SUMS.txt**. Incluye .NET y el icono oficial. Mantiene el soporte Windows/macOS/Ubuntu y las publicaciones históricas, sin añadir paquetes de Arch/Omarchy o RPM.

### Verificación

Compilación local sin errores ni advertencias y **514 pruebas automatizadas aprobadas**. Se comprueban los estados de asistencia, semanas activas/retiradas, totales por corte, actualización sin lecturas adicionales, filtros y estados vacíos, selección de campos y paginación del PDF. Los ejemplos utilizan exclusivamente datos ficticios. El pipeline vuelve a compilar y probar en las cinco plataformas/arquitecturas, valida los paquetes y verifica los hashes descargados antes de hacer estable la Release. Las pruebas manuales continúan a cargo del docente.

Los instaladores no incluyen bases, materias ni estudiantes de prueba. Windows continúa sin firma oficial y macOS sin Apple Developer ID/notarización, conservando firma ad hoc de integridad. Licencia MIT.

[Repositorio](https://github.com/danielpose1996-stack/SIGNA) · [Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) · [Guía de actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).
