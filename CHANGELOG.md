# Historial de cambios de SIGNA

## 1.6.7 — 7 de noviembre de 2026

- Diseño unificado en Inicio, Materias, Notas, Asistencias, vistas consolidadas, Respaldos y diálogos de SIGNA.
- El color elegido en **Personalizar apariencia** actualiza los degradados de la barra lateral y los encabezados, con contraste adaptado. Los cambios se previsualizan al elegir el color y se recuperan al reiniciar después de guardar.
- Las tablas conservan espacio para el listado en ventanas de poca altura. El selector de cortes, los controles de las tablas y los estados de selección tienen un estilo común.
- Se conservan los flujos académicos, las rutas de datos, las preferencias y los respaldos. Actualización integrada disponible para Windows, macOS y Ubuntu AppImage.

## 1.6.6 — 6 de noviembre de 2026

- Menú de acciones en el encabezado de cada actividad directa y subactividad editable de Notas. **Pegar notas** conserva el orden y el filtro de las filas visibles.
- **Asignar nota a todos** admite valores de 0,0 a 5,0 con coma o punto decimal. Incluye todo el grupo, indica los reemplazos y requiere confirmación. Guarda el lote en una sola operación y actualiza las definitivas y la nota consolidada.
- Distribución para Windows, macOS y Ubuntu. El empaquetado dedicado a Debian se retiró según la solicitud del docente.

## Retiro de Debian — 6 de noviembre de 2026

- Se retiran los instaladores exclusivos de Debian 13, su manifiesto y el flujo de empaquetado dedicado. La distribución Linux disponible es Ubuntu 24.04 LTS.

## 1.6.5 — 3 de noviembre de 2026

- Nueva pestaña **Asistencia consolidada** junto a Notas, Asistencias y Nota consolidada. Muestra a los estudiantes, sus ausencias (A) en cada corte académico y el **Total de faltas**, respetando los nombres personalizados y cualquier cantidad de cortes.
- Conteo de A solo en semanas activas de clase o actividades; P, J, T, sin clase, pendientes y semanas retiradas no suman faltas. Se actualiza automáticamente al guardar registros de cualquier corte, retirar/recuperar semanas o modificar cortes y estudiantes.
- Proyección en memoria con actualización del alumno afectado y búsqueda agrupada sin consultas ni escrituras adicionales a SQLite. Tabla con filas alternadas, cabecera fija, nombres completos, virtualización y desplazamiento horizontal.
- El **PDF de Nota consolidada** incluye una sección de tarjetas para alumnos Crítico y Delicado, críticos primero, con nombre completo, correo si existe y nota mínima para aprobar. Conserva el diseño de las tarjetas y pagina sin cortarlas, con contexto y numeración completos.
- El correo se incluye únicamente en esas tarjetas de seguimiento. La tabla de calificaciones no lleva contactos y el PDF no incorpora documentos, teléfonos ni IDs internos. Si no hay casos de riesgo, no agrega una página vacía. La interfaz de exportación informa la inclusión de las tarjetas con correo.
- Sin cambios de esquema, migraciones, ubicación de datos ni dependencias; conserva el agregado académico 8, las calificaciones, porcentajes, políticas de asistencia y respaldos. Interfaz y PDF revisados con datos ficticios, incluida escala 125 %; 514 pruebas automatizadas locales aprobadas antes de publicar.

**Actualizar:** desde SIGNA 1.6.0–1.6.4 con Velopack, pulsa Buscar actualizaciones → Actualizar SIGNA. Windows 10/11 x64, macOS 15+ Intel/Apple Silicon y Ubuntu 24.04 LTS x64/ARM64. En Ubuntu la actualización integrada corresponde a AppImage; los paquetes .deb se actualizan con apt. Sin certificados nuevos de firma; las pruebas manuales siguen a cargo del docente.

## 1.6.4 — 29 de octubre de 2026

- Selector desplegable de cortes en Notas y Asistencias, con configuración para agregar, renombrar y eliminar cortes con confirmación. Conserva los porcentajes existentes, la numeración y los registros de cada corte; impide eliminar el último y solicita ajustar los pesos sin redistribuirlos automáticamente.
- «Nota mínima» antes de «Nota final» en Nota consolidada: calcula el promedio ponderado necesario en los cortes pendientes para alcanzar 3,0. Un corte está completo para un estudiante cuando tiene evaluaciones válidas y todas sus notas guardadas. Un 0,0 explícito cuenta como definitivo; una celda vacía sigue pendiente. Sin cortes completos o con porcentajes pendientes no muestra un cálculo.
- Estados académicos automáticos: Aprobado asegurado, Normal, Delicado, Crítico y No alcanzable. Usa valores exactos para clasificar y muestra la mínima hacia arriba a dos decimales, sin cambiar las calificaciones ni los cálculos existentes de la nota final.
- Tarjetas solo para alumnos críticos y delicados, críticos primero, con nombre, correo si existe y mínima requerida. Desplazamiento con virtualización para listados grandes y sección desplegable en ventanas con poca altura.
- Reporte PDF de Nota consolidada con todos los estudiantes, nombres completos, cortes y porcentajes personalizados, notas, final, mínima y estado. Excluye documentos, correos, teléfonos e identificadores internos; permite elegir destino y confirma reemplazos.
- Nombres personalizados conservados en reportes, copia de parámetros, SQLite y respaldos. Agregado académico versión 8: migración desde versión 7 que preserva las ponderaciones internas de subactividades, notas y asistencias. No cambia tablas SQL, rutas de datos, respaldos ni archivos del actualizador.
- Guardas para conservar ediciones pendientes al cambiar/configurar cortes o generar el consolidado. Interfaz revisada con datos ficticios en ventanas normales, pequeñas y escala 125 %; 504 pruebas automatizadas locales aprobadas antes de publicar.

**Actualización:** desde SIGNA 1.6.0–1.6.3 con Velopack utiliza Buscar actualizaciones → Actualizar SIGNA. Conserva un respaldo local antes de actualizar. Las versiones anteriores no abren el agregado 8; para volver atrás se necesita un respaldo anterior compatible. Windows 10/11 x64, macOS 15+ Intel/Apple Silicon y Ubuntu 24.04 LTS x64/ARM64. Sin nuevos certificados de firma; las pruebas manuales siguen a cargo del docente.

## 1.6.3 — 26 de octubre de 2026

- «Eliminar lista» en las barras de Notas y Asistencias, con papelera, estilo de acción destructiva y confirmación que muestra la materia y los conteos afectados. Elimina la lista completa aunque haya un filtro activo.
- Borrado de estudiantes, datos de contacto y registros de notas/asistencias de todos los cortes en un único guardado transaccional. Conserva materia, semestre, cortes, pesos, actividades, subactividades, semanas, fechas, configuraciones e historial de configuración; no afecta otras materias.
- Cancelación/Escape sin cambios, validación de confirmaciones obsoletas, control de operaciones simultáneas y errores de escritura sin borrados parciales. Las ediciones pendientes se conservan y deben resolverse antes de continuar.
- Actualización inmediata de Notas, Asistencias, selección, conteos y Nota consolidada. Se limpia la búsqueda y permanecen disponibles la importación y el registro de estudiantes para cargar un nuevo listado.
- Barra adaptable en ventanas pequeñas y escala 125 %, manteniendo el texto «Eliminar lista» y los accesos existentes. Sin cambios de esquema, reglas académicas ni ubicación de los datos; conserva respaldos y actualizaciones Velopack.

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
