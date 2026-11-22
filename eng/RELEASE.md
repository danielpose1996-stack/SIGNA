**SIGNA 1.6.11 — Buscador de estudiantes por semestre**

- **Encuentra sus materias desde Mis Materias.** Selecciona el período y escribe parte del nombre o apellido en **Buscar estudiante en el período**. Los resultados muestran el nombre completo, las asignaturas, sus códigos y grupos.
- **Búsqueda en tiempo real.** Se actualiza al escribir, sin pulsar Buscar ni Enter, e ignora diferencias de mayúsculas y tildes. **Limpiar** retira el filtro y los resultados.
- **Exclusivamente del semestre seleccionado.** Por ejemplo, si eliges 2026-2, solo se muestran coincidencias de 2026-2. Al cambiar de período se borran las anteriores y se consulta nuevamente; una respuesta atrasada no mezcla semestres.
- **Consulta informativa y fluida.** El filtrado se realiza en memoria, sin consultas a SQLite por cada letra. La lista admite desplazamiento y virtualización. Mantiene separados los documentos distintos y los registros sin documento no se unen solo por el nombre.

La consulta no muestra documentos, correos, teléfonos, notas ni asistencias y no modifica los registros. Esta actualización conserva las materias, estudiantes, calificaciones, configuraciones y respaldos existentes, así como el agregado académico 9 y sus ubicaciones.

### Actualización y descargas

Desde **SIGNA 1.6.0–1.6.10 con Velopack**, abre **Buscar actualizaciones → Actualizar SIGNA**.

- **Windows 10/11 x64** · `SIGNA-1.6.11-windows-x64-setup.exe`.
- **macOS 15+** · `SIGNA-1.6.11-osx-x64.dmg` para Intel y `SIGNA-1.6.11-osx-arm64.dmg` para Apple Silicon.
- **Ubuntu 24.04 LTS** · `SIGNA-1.6.11-linux-x64.AppImage` o `SIGNA-1.6.11-linux-arm64.AppImage` para actualización integrada. Los paquetes `.deb` se actualizan con apt.

Genera un respaldo local antes de actualizar. Se mantiene el formato académico de 1.6.10 y no es necesario trasladar ni volver a importar los datos.

Consulta **INSTALACION.md** y **SHA256SUMS.txt**. Los archivos `releases.<canal>.json` y `-full.nupkg` corresponden al actualizador. Licencia MIT. La distribución conserva las condiciones de firma de las versiones anteriores.

[Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) · [Guía de actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).
