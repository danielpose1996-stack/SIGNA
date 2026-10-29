**SIGNA 1.6.4 — Cortes personalizados y seguimiento del riesgo académico**

### Novedades

- **Selector de cortes renovado:** desplegable en Notas y Asistencias y botón de configuración para agregar, renombrar y eliminar cortes. Puedes usar nombres como «Primer período». Conserva los cortes y porcentajes existentes; no permite eliminar el último. Toda eliminación requiere confirmación y solicita revisar los porcentajes sin redistribuirlos automáticamente.
- **Nota mínima para aprobar con 3,0:** nueva columna antes de Nota final en Nota consolidada. Calcula el promedio ponderado que se necesita en los cortes pendientes a partir del aporte de los cortes completos. Sin cortes completos o con porcentajes pendientes, no muestra una mínima numérica.
- **Estados académicos automáticos:** Aprobado asegurado, Normal (hasta 3,0), Delicado (más de 3,0 hasta 4,0), Crítico (más de 4,0 hasta 5,0) y No alcanzable (más de 5,0, o sin peso restante y sin alcanzar 3,0).
- **Tarjetas de atención académica:** muestran solo casos críticos y delicados, críticos primero, con nombre completo, correo si existe y nota mínima. Las tarjetas se desplazan y virtualizan para listas grandes; en ventanas de poca altura se pueden desplegar para conservar espacio en la tabla.
- **PDF de Nota consolidada:** incluye todos los estudiantes, sus nombres, notas por corte, porcentajes, nota final, mínima y estado. No incluye documentos, correos, teléfonos ni identificadores internos. Permite elegir destino y solicita confirmación antes de reemplazar un PDF existente.

### Qué significa un corte completo

Un corte está completo **para cada estudiante** cuando tiene evaluaciones válidas y todas sus notas guardadas. **Un 0,0 registrado cuenta como definitivo; una celda vacía sigue pendiente.** Esto permite distinguir un cero real de una nota que todavía no se ha diligenciado.

La nota mínima usa solo el aporte ponderado de los cortes completos y representa el promedio ponderado necesario en los restantes. Se presenta hacia arriba a dos decimales para no subestimar lo requerido. La clasificación usa el valor exacto. La nota final conserva la regla actual de vacíos como 0,0 únicamente para calcular, sin escribir ceros ni modificar calificaciones originales.

### Correcciones y conservación de datos

- Se conservan las ediciones pendientes al cambiar o configurar cortes y antes de generar el reporte consolidado. Los errores de guardado mantienen los borradores y se informan sin cierres ni excepciones silenciosas.
- Los nombres personalizados se conservan en reportes, copia de parámetros y respaldos. Los nombres predeterminados siguen la numeración al eliminar cortes; las notas y asistencias mantienen su asociación correcta.
- El agregado académico pasa a **versión 8**. La migración desde versión 7 conserva los promedios ponderados de subactividades, sus porcentajes y los registros existentes. No cambia las tablas SQLite, rutas de datos, carpetas de respaldos ni archivos del actualizador.
- Distribución adaptable revisada con datos ficticios, incluida escala 125 %. La tabla mantiene desplazamiento horizontal y los casos de riesgo no ocupan todo el espacio de trabajo.

### Cómo actualizar y descargar

Desde **SIGNA 1.6.0–1.6.3 con Velopack**, pulsa **Buscar actualizaciones → Actualizar SIGNA**. No necesitas desinstalar. Antes de actualizar, genera un respaldo local. Una versión antigua no abre el agregado 8; para volver atrás se necesita un respaldo anterior compatible. Las copias existentes no se eliminan. Sin internet puedes seguir trabajando.

- **Windows 10/11 x64:** `SIGNA-1.6.4-windows-x64-setup.exe`, con licencia MIT y selección de carpeta.
- **Ubuntu 24.04 LTS x64/ARM64:** `SIGNA-1.6.4-linux-x64.AppImage`, `SIGNA-1.6.4-linux-arm64.AppImage` y `.deb` de ambas arquitecturas. La actualización integrada corresponde a AppImage; los paquetes `.deb` se actualizan instalando el nuevo paquete con apt.
- **macOS 15+, Intel/Apple Silicon:** `SIGNA-1.6.4-osx-x64.dmg` y `SIGNA-1.6.4-osx-arm64.dmg`.
- Los cinco feeds `releases.<canal>.json` y cinco archivos `-full.nupkg` son para el actualizador; no son instaladores para abrir manualmente.

Consulta **INSTALACION.md** y **SHA256SUMS.txt**. Incluye .NET y el icono oficial. Mantiene el soporte actual Windows/macOS/Ubuntu y las publicaciones históricas; no añade paquetes de Arch/Omarchy o RPM.

### Verificación

Compilación local sin errores ni advertencias y **504 pruebas automatizadas aprobadas**. Se verificaron migración 7→8, límites de riesgo, cero explícito frente a pendiente, guardado atómico, privacidad y paginación PDF, respaldo/restauración en otra base temporal y virtualización con 1200 alumnos ficticios. El pipeline vuelve a compilar y probar en las cinco plataformas/arquitecturas, comprueba los paquetes y verifica los hashes descargados antes de publicar como estable. Las pruebas manuales siguen a cargo del docente.

Los instaladores no incluyen materias, estudiantes ni bases de prueba. Windows continúa sin firma oficial y macOS sin Apple Developer ID/notarización, conservando firma ad hoc de integridad. Licencia MIT.

[Repositorio](https://github.com/danielpose1996-stack/SIGNA) · [Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) · [Guía de actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).
