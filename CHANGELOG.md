# Historial de cambios de SIGNA

## 1.6.2 — 23 de octubre de 2026

- Respaldos automáticos después de escrituras confirmadas: agrupa cambios durante 30 segundos y limita la espera a dos minutos bajo guardados continuos, sin consultas por pulsación ni trabajo de copia en el hilo de interfaz.
- Misma carpeta predeterminada de los manuales; máximo cinco automáticos verificados, identificados mediante un índice de propiedad y hashes. Solo se rota después de verificar la nueva copia; nunca se eliminan manuales, preventivos ni archivos desconocidos.
- Estado y errores visibles en Respaldos, reintento espaciado si falla, coordinación con restauración, limpieza y actualización, y respaldo de cambios pendientes al cerrar normalmente. Las copias automáticas se restauran en otro equipo igual que las manuales.

- Respaldos locales nuevos sin contraseña ni cifrado, con longitud, SHA-256 y validación física/referencial/académica de SQLite. Lectura compatible de los respaldos antiguos con su contraseña original.
- Carpeta predeterminada guardada y modificable. «Generar respaldo» crea una copia fechada con nombre único directamente allí; una carpeta no disponible se informa sin cambiar de destino automáticamente.
- «Limpiar datos de este equipo» con confirmación, transacción y reinicio del catálogo/perfil/configuraciones. Conserva los respaldos de cualquier carpeta y los archivos del actualizador; bloquea operaciones simultáneas y cierre mientras trabaja con los datos.
- Restauración normal y de recuperación al iniciar compatibles con ambos formatos; copia preventiva sin contraseña y recarga del catálogo después de restaurar o limpiar.

## 1.6.1 — 21 de octubre de 2026

- Botón lateral «Buscar actualizaciones» con icono vectorial, fondo, borde y estados de puntero/pulsación/foco. Conserva su comando y la ventana de actualización existentes, con colores compatibles con la personalización.
- Soporte futuro: Windows 10/11 x64, macOS 15+ Intel/Apple Silicon y Ubuntu 24.04 LTS x64/ARM64. Ubuntu conserva AppImage actualizable y `.deb`; se retiran RPM y Arch/Omarchy del empaquetado y ya no se ofrece soporte Debian. Las Releases históricas permanecen intactas.
- Versión de parche 1.6.1 y cinco canales Velopack conservados para recibir la actualización desde 1.6.0. Manifiesto y workflow ajustados a siete distribuciones y 19 adjuntos, con rechazo de paquetes retirados o datos privados.
- Sin cambios en cálculos académicos, SQLite, preferencias ni respaldos.

## 1.6.0 — 21 de octubre de 2026

- **Revisión del instalador Windows, misma versión 1.6.0:** asistente con licencia MIT y selección de carpeta, sobre el Setup original de Velopack. Protege carpetas ajenas y datos/respaldos dentro del destino, mantiene el único desinstalador de Velopack y actualiza el SHA-256 del EXE. No cambian el tag, las funciones académicas, los cinco feeds/nupkg ni los instaladores macOS/Linux. Volver a descargar el EXE permite utilizar el asistente nuevo.

- Actualizaciones estables mediante Velopack 1.2.161 y GitHub Releases oficiales. Comprobación al abrir sin bloquear el trabajo offline, búsqueda manual, aviso con ambas versiones y descarga por acción del docente con progreso/cancelación, instalación y reinicio controlado.
- Verificación de tamaño/SHA-256 después de descargar y antes del reinicio, incluida la caché existente. Versiones/canales por sistema y arquitectura, sin versiones preliminares ni downgrade; errores y reintento sin cerrar SIGNA.
- Guardas ante guardados, notas pendientes, otra instancia, rutas inseguras o SQLite inválido. Conserva ubicación y esquema de SQLite, preferencias y respaldos locales cifrados; no se incluyen datos personales en los paquetes.
- Windows Setup preparado por Velopack, DMG macOS con helper/manifiesto y AppImage Linux actualizable. Conserva DEB, RPM y el paquete Arch/Omarchy, administrados mediante sus gestores. Feeds/paquetes completos y validación de integridad antes de publicar.

Las versiones hasta 1.5.0 necesitan instalar una vez la edición nueva para recibir actualizaciones internas. En Linux esa función usa AppImage. [Procedimiento, alcance y limitaciones](docs/ACTUALIZACIONES.md). Descarga los diez instaladores/distribuciones desde [SIGNA 1.6.0](https://github.com/danielpose1996-stack/SIGNA/releases/tag/v1.6.0), con cinco feeds y paquetes de actualización verificados. Sin certificados nuevos de firma. Las comprobaciones manuales del ciclo real siguen a cargo del docente.

## 1.5.0 — 21 de octubre de 2026

- Nuevo paquete nativo para Arch Linux y Omarchy x86_64, instalable con Pacman, con .NET incluido, icono oficial, acceso del menú y dependencias X11/XWayland. Construcción con makepkg y comprobación de instalación/desinstalación en contenedor oficial Arch, conservando datos de usuario.
- Copiar parámetros en las tarjetas de Materias y pegarlos opcionalmente al crear otra asignatura, incluso en otro semestre. Incluye cortes, porcentajes, actividades, subactividades y nota de asistencia habilitada, con resumen previo y configuración independiente. Excluye estudiantes, contactos, calificaciones, semanas y registros de asistencia; guardado atómico y sin modificar el origen.
- Selector HSV más fluido: cambios de saturación/brillo conjuntos, vista previa agrupada por intervalos, aplicación final al soltar y cancelación de cambios pendientes. Recursos agrupados y paleta Fluent reutilizada; dibujo del selector reutiliza pinceles y degradados.
- Contraste automático coherente en los fondos reales de botones primarios y cortes seleccionados, incluidos cursor encima, pulsación y deshabilitado. Bindings compilados en Personalización.
- Inicio con pantalla de carga e inicialización del almacenamiento fuera del hilo visual. Estadísticas progresivas con cancelación de lecturas obsoletas y reutilización por revisión de cada materia.
- Cambio de corte desde la interfaz asíncrono, búsqueda agrupada sin consultar SQLite por pulsación, modelos de celdas y detalles creados cuando se utilizan y mediciones de columnas compartidas entre tablas. Se conservan datos, cálculos, transacciones y esquema SQLite.

**Correcciones:** evita reaplicar el tema completo por cada evento del selector, corrige texto e iconos sin contraste sobre colores claros y reduce bloqueos del hilo visual al cargar datos. Omite consultas repetidas de estadísticas sin cambios, detiene cargas obsoletas y conserva el filtro y orden correctos al pegar calificaciones. No se alteran notas, reglas académicas, formato SQLite ni ubicación de datos.

**Actualización y descarga:** crea un respaldo local cifrado antes de actualizar. Los instaladores incluyen .NET y el icono oficial, conservan los datos existentes y no contienen bases de datos ni estudiantes de prueba. Descarga los ocho instaladores de Windows, Linux (DEB/RPM y Arch/Omarchy) y macOS desde [SIGNA 1.5.0](https://github.com/danielpose1996-stack/SIGNA/releases/tag/v1.5.0). Se distribuyen sin firma oficial de Windows, Apple Developer ID/notarización ni firma de paquete Arch. Las pruebas manuales quedan a cargo del docente.

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
