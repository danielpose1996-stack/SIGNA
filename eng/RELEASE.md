**SIGNA 1.5.0 — Sistema Integral de Gestión de Notas y Asistencias**

### Novedades

- **Instalador para Arch Linux y Omarchy x64:** paquete nativo `.pkg.tar.zst`, con .NET incluido, icono oficial y entrada en el menú. Se instala con `sudo pacman -U ./SIGNA-1.5.0-archlinux-x64.pkg.tar.zst`. No requiere AUR ni convertir un DEB/RPM; declara XWayland para escritorios Wayland como Hyprland. La instalación y desinstalación se comprueban en un contenedor Arch; la revisión visual en Omarchy queda a cargo del docente.
- **Copiar parámetros entre materias:** cada tarjeta de Mis Materias incorpora **Copiar parámetros**. Puedes elegir una materia de cualquier semestre y, al crear otra, pulsar **Pegar parámetros** y revisar el resumen antes de guardar.
- Se conservan los cortes y sus porcentajes, las actividades principales, las subactividades, sus ponderaciones y la configuración de nota de asistencia cuando está habilitada. Se mantienen la nota directa y los promedios simple o ponderado.
- **La materia nueva es independiente:** mantiene su propio nombre, código, escuela, grupo y período. Sus actividades reciben identificadores nuevos y puedes editarlas sin afectar el origen.
- **Privacidad:** no se copian estudiantes, documentos, contactos, calificaciones, semanas ni registros de asistencia. La copia se conserva temporalmente hasta cerrar SIGNA; copiar otra materia la reemplaza. **Usar configuración inicial** permite quitarla antes de guardar.

### Errores corregidos y mejoras de rendimiento

- **Selector HSV más fluido:** se agrupan las actualizaciones visuales durante el arrastre, se omiten colores repetidos y se reutilizan recursos de dibujo y del tema. Al soltar o guardar se aplica el último color; cancelar descarta las actualizaciones pendientes. La vista previa no escribe preferencias en SQLite.
- **Texto legible con colores claros:** se corrigió el contraste de texto e iconos sobre el fondo real de botones primarios y cortes seleccionados, incluidos los estados de cursor encima, pulsado y deshabilitado. Se mantienen el color elegido y los colores académicos.
- **Arranque con respuesta visual:** pantalla de carga y preparación del almacenamiento fuera del hilo de la interfaz, conservando la recuperación ante errores. Cerrar la pantalla de carga evita que aparezca después una ventana principal inesperada.
- **Estadísticas sin lecturas repetidas innecesarias:** se reutilizan resultados de materias sin cambios, se actualizan las modificadas y se detienen las cargas obsoletas. Actualizar sigue forzando una lectura nueva.
- **Búsqueda y cambio de corte:** los buscadores agrupan la escritura rápida sin consultar SQLite por cada tecla. El cambio de corte prepara las tablas fuera del hilo visual; se reutilizan mediciones de columnas y se crean celdas/detalles cuando se necesitan.
- **Asignación de notas preservada:** ordenar o pegar calificaciones aplica primero el filtro pendiente, respetando la lista visible. Se conservan borradores, asociaciones por estudiante y el control de cambios concurrentes.

### Integridad y actualización

Copiar o pegar parámetros no modifica la base de datos. La materia completa se guarda mediante la transacción existente; si falla, no queda un registro parcial. Los porcentajes pendientes de ajuste se conservan y se advierten, sin redistribuirlos automáticamente.

Esta actualización no cambia el esquema SQLite, las reglas académicas, la ubicación de datos ni los cálculos existentes. Conserva estudiantes, calificaciones, asistencias y preferencias. Antes de instalar, crea un respaldo local cifrado, conserva su contraseña y cierra SIGNA. Los instaladores no incluyen bases de datos, materias ni estudiantes de prueba.

### Descargas

| Sistema | Archivo |
| --- | --- |
| Windows 10/11 x64 | `SIGNA-1.5.0-windows-x64-setup.exe` |
| Ubuntu/Debian x64 | `SIGNA-1.5.0-linux-x64.deb` |
| Ubuntu/Debian ARM64 | `SIGNA-1.5.0-linux-arm64.deb` |
| Fedora y derivados x64 | `SIGNA-1.5.0-linux-x64.rpm` |
| Fedora y derivados ARM64 | `SIGNA-1.5.0-linux-arm64.rpm` |
| Arch Linux / Omarchy x64 | `SIGNA-1.5.0-archlinux-x64.pkg.tar.zst` |
| macOS 15 o posterior, Intel | `SIGNA-1.5.0-osx-x64.dmg` |
| macOS 15 o posterior, Apple Silicon | `SIGNA-1.5.0-osx-arm64.dmg` |

Incluyen .NET y el icono oficial. Consulta **INSTALACION.md** y comprueba **SHA256SUMS.txt**. Linux requiere las bibliotecas declaradas por el paquete y X11/XWayland. Las actualizaciones y desinstalaciones conservan los datos del usuario.

### Validación y firma

392 pruebas automatizadas cubren dominio, aplicación, SQLite temporal y presentación Avalonia Headless. El proceso de publicación compila, ejecuta la suite y verifica los paquetes en Windows, Linux x64/ARM64 y macOS Intel/Apple Silicon; comprueba también la instalación/desinstalación del paquete Arch en un contenedor oficial y ejecuta tres pruebas del manifiesto de distribución. Solo publica al completar los ocho instaladores y comprobar sus hashes. Las pruebas manuales quedan a cargo del docente. Las optimizaciones verifican operaciones y resultados; no prometen un tiempo de arranque fijo para todos los equipos.

**Sin firma oficial de Windows ni Apple Developer ID/notarización**, según la distribución actual. Windows/macOS pueden mostrar advertencias; la firma ad hoc de macOS comprueba integridad sin acreditar al desarrollador. No es necesario desactivar globalmente las protecciones del sistema.

El paquete Arch se distribuye sin firma de repositorio; verifica su SHA-256 y utiliza la instalación local de Pacman sin desactivar globalmente sus comprobaciones.

Licencia MIT. [Repositorio oficial](https://github.com/danielpose1996-stack/SIGNA) · [Historial de cambios](https://github.com/danielpose1996-stack/SIGNA/blob/main/CHANGELOG.md).
