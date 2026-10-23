**SIGNA 1.6.2 — Respaldos automáticos y gestión local simplificada**

### Novedades y correcciones

- **Respaldos automáticos en segundo plano:** después de guardar cambios, SIGNA espera 30 segundos para agruparlos. Si continúas guardando, la espera se limita a dos minutos. No se generan copias por cada pulsación ni cuando no hay cambios.
- **La misma carpeta para manuales y automáticos:** elige una vez la carpeta en Respaldos; puedes cambiarla cuando quieras. No se solicita ubicación en cada copia ni se inventa otro destino si la carpeta está desconectada.
- **Máximo cinco automáticos:** al verificar una nueva copia, se elimina la automática más antigua que corresponda. Un índice de propiedad y hashes separa los automáticos de los manuales y preventivos, que nunca entran en la rotación. Cambiar de carpeta conserva las copias anteriores.
- **Copias completas y portables:** semestres, materias, estudiantes y contactos, cortes, porcentajes, actividades, subactividades, notas, asistencias, fechas y preferencias. Se usa la API consistente de SQLite y se verifican el contenido, la integridad y las relaciones antes de publicar cada copia.
- **Respaldos nuevos sin contraseña:** se conserva la lectura de archivos cifrados anteriores con su contraseña original. Protege el acceso a la carpeta de los archivos nuevos, porque ya no están cifrados.
- **Estado y errores visibles:** Respaldos informa el resultado y los problemas de carpeta, permisos, espacio o integridad. Los errores no interrumpen los guardados académicos; se reintenta con espera. Restauración, limpieza y actualización esperan a que termine la copia en curso. Al cerrar normalmente se intenta respaldar los cambios pendientes.
- **Limpiar datos de este equipo:** confirmación previa y reinicio del catálogo y configuraciones, conservando todos los respaldos y archivos del actualizador. La restauración mantiene su verificación y copia preventiva.

### Cómo actualizar y activar los automáticos

1. Guarda tus cambios y conserva un respaldo antes de actualizar. Cierra otras instancias de SIGNA.
2. Desde **1.6.0/1.6.1 con Velopack**, pulsa **Buscar actualizaciones → Actualizar SIGNA**. Se descarga, verifica e instala 1.6.2 y se reinicia conservando los datos.
3. Abre **Respaldos → Elegir carpeta**. La selección queda recordada; tanto Generar respaldo como los automáticos usan esa ubicación. Las copias anteriores siguen siendo restaurables.
4. Continúa trabajando y guardando normalmente. Consulta el estado automático en Respaldos. Puedes restaurar cualquier .aulabk automático en otro equipo mediante Elegir copia y restaurar.

Sin internet SIGNA sigue funcionando. La actualización interna de Ubuntu corresponde a AppImage; el paquete .deb se actualiza descargando la nueva edición e instalándola con apt. Las ediciones hasta 1.5.0 necesitan instalar una vez la edición preparada con Velopack.

### Descargas

- **Windows 10/11 x64:** `SIGNA-1.6.2-windows-x64-setup.exe`, asistente con licencia MIT y selección de carpeta.
- **Ubuntu 24.04 LTS x64/ARM64:** `SIGNA-1.6.2-linux-x64.AppImage`, `SIGNA-1.6.2-linux-arm64.AppImage` y paquetes `.deb` de ambas arquitecturas.
- **macOS 15+, Intel/Apple Silicon:** `SIGNA-1.6.2-osx-x64.dmg` y `SIGNA-1.6.2-osx-arm64.dmg`.
- Cinco feeds `releases.<canal>.json` y cinco paquetes `-full.nupkg` para el actualizador; no son instaladores que debas abrir manualmente.

Los instaladores incluyen .NET y el icono oficial. Consulta **INSTALACION.md** y **SHA256SUMS.txt**. No se generan paquetes Arch/Omarchy ni RPM; se conservan las Releases históricas.

### Validación y límites

El pipeline compila y prueba en cinco sistemas/arquitecturas antes de publicar, con pruebas de instalación y empaquetado nativas, manifiesto completo y verificación de los archivos descargados después de subirlos. Las pruebas de respaldos usan exclusivamente datos ficticios y bases temporales; comprueban agrupación, rotación, fallos, concurrencia, integridad, portabilidad y coordinación con la interfaz. La presentación se revisa mediante capturas Avalonia Headless. Las pruebas manuales finales y el ciclo real entre versiones instaladas corresponden al docente.

No se incluyen materias, bases de datos ni estudiantes de prueba. Windows sigue sin firma oficial y macOS sin Apple Developer ID/notarización; se mantiene firma ad hoc de integridad. Licencia MIT.

[Repositorio](https://github.com/danielpose1996-stack/SIGNA) · [Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md) · [Actualizaciones](https://github.com/danielpose1996-stack/SIGNA/blob/main/docs/ACTUALIZACIONES.md).
