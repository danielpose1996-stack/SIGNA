**SIGNA 1.6.9 — Candados por corte y copia de notas**

- **Más estudiantes en pantalla.** Contrae el encabezado y las opciones superiores de Notas y Asistencias. El botón para contraer y expandir permanece en la misma esquina superior derecha, y el estado se conserva al navegar durante la sesión.
- **Materias en mayúsculas.** Código, nombre, escuela y grupo se convierten automáticamente. Las materias existentes se normalizan con una copia SQLite preventiva verificada, conservando sus identificadores y registros académicos.
- **Candado por corte.** Bloquea o desbloquea con confirmación junto al selector o en Configurar cortes. El cierre protege notas, actividades, subactividades, semanas y asistencias. Puedes seguir consultando, generando PDF y copiando notas. El estado se conserva al reiniciar y en los respaldos.
- **Copiar notas con un clic.** Usa la rueda dentada de una actividad o subactividad, o el encabezado de Nota de asistencia. Copia solo los números, uno por línea, en el orden y filtro visibles. Pega con Ctrl+V en Word, Bloc de notas o una hoja de cálculo. Las notas pendientes dejan líneas vacías. Los ceros explícitos se copian como 0,00.
- **Controles adaptables.** La barra de cortes conserva sus acciones accesibles en ventanas estrechas y con escala al 125 %.

### Actualización y descargas

Desde **SIGNA 1.6.0–1.6.8 con Velopack**, abre **Buscar actualizaciones → Actualizar SIGNA**.

- **Windows 10/11 x64** · `SIGNA-1.6.9-windows-x64-setup.exe`.
- **macOS 15+** · `SIGNA-1.6.9-osx-x64.dmg` para Intel y `SIGNA-1.6.9-osx-arm64.dmg` para Apple Silicon.
- **Ubuntu 24.04 LTS** · `SIGNA-1.6.9-linux-x64.AppImage` o `SIGNA-1.6.9-linux-arm64.AppImage` para actualización integrada. Los paquetes `.deb` se actualizan con apt.

Genera un respaldo local antes de actualizar. Los candados utilizan el agregado académico 9. Conserva una copia anterior si necesitas volver a una edición que utiliza el formato previo.

Consulta **INSTALACION.md** y **SHA256SUMS.txt**. Los archivos `releases.<canal>.json` y `-full.nupkg` corresponden al actualizador. Licencia MIT. La distribución conserva las condiciones de firma de las versiones anteriores.

[Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) · [Guía de actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).
