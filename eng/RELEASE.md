**SIGNA 1.6.10 — Parche de corrección de Nota consolidada**

- **3,00 significa Aprobado.** Cuando todos los cortes del estudiante están completos, la aprobación utiliza la definitiva mostrada a dos decimales. Se corrige el caso de un resultado exacto de 2,998 que mostraba 3,00 y «No alcanzable».
- **Estados finales claros.** Las materias completadas muestran **Aprobado** o **Reprobado**. Las proyecciones de riesgo y la nota mínima conservan sus reglas cuando hay cortes pendientes.
- **Colores y PDF coherentes.** El fondo de las definitivas corresponde al valor visible y el PDF de Nota consolidada comparte el mismo estado que la tabla.

Este parche conserva las calificaciones originales, los porcentajes, la precisión de los cálculos y el agregado académico 9. Las materias, estudiantes, asistencias, preferencias y respaldos mantienen sus ubicaciones.

### Actualización y descargas

Desde **SIGNA 1.6.0–1.6.9 con Velopack**, abre **Buscar actualizaciones → Actualizar SIGNA**.

- **Windows 10/11 x64** · `SIGNA-1.6.10-windows-x64-setup.exe`.
- **macOS 15+** · `SIGNA-1.6.10-osx-x64.dmg` para Intel y `SIGNA-1.6.10-osx-arm64.dmg` para Apple Silicon.
- **Ubuntu 24.04 LTS** · `SIGNA-1.6.10-linux-x64.AppImage` o `SIGNA-1.6.10-linux-arm64.AppImage` para actualización integrada. Los paquetes `.deb` se actualizan con apt.

Genera un respaldo local antes de actualizar. El parche mantiene el formato académico de 1.6.9 y no requiere trasladar ni volver a importar los datos.

Consulta **INSTALACION.md** y **SHA256SUMS.txt**. Los archivos `releases.<canal>.json` y `-full.nupkg` corresponden al actualizador. Licencia MIT. La distribución conserva las condiciones de firma de las versiones anteriores.

[Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) · [Guía de actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).
