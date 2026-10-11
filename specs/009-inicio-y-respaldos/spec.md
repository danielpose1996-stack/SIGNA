# 009 — Entrega A: Inicio y navegación

Estado: entrega A autorizada por «empieza» el 2026-10-11, después de presentar el plan. B y C quedan para incrementos separados.

## Alcance autorizado

Abrir en Inicio sin cuentas ni conexión externa. Habilitar Inicio, Mis Materias y Respaldos en el menú; conservar Notas/Asistencias/rúbricas. El menú refleja la sección seleccionada. El encabezado, las pestañas y el selector de corte de una materia se ocultan en páginas globales. No modificar persistencia, reglas académicas ni datos existentes.

Inicio muestra materias realmente disponibles, datos de grupo/período y accesos para abrir la materia o ir a Respaldos. Con el almacenamiento actual solo existe una materia de demostración, claramente identificada. Mis Materias muestra ese mismo catálogo sin presentar un CRUD ya terminado. Si no existe una materia con nombre, mostrar estado vacío y no ofrecer una apertura inválida.

La página Respaldos es informativa en esta entrega: explica archivo local, restauración y Drive opcional; indica que aún no están implementados y no muestra un éxito ni conecta cuentas. No inventar un registro de último respaldo. Se retira la identidad de ejemplo Juan Pérez del shell y se muestra Uso local / Sin cuenta requerida.

## Criterios comprobables

- INI-01: Arranque en Inicio y acceso a las secciones disponibles sin autenticación. Sin llamadas externas añadidas.
- INI-02: Un único apartado activo del menú. Inicio, Mis Materias y Respaldos no muestran encabezado, pestañas ni cortes de una materia; al abrirla sí aparecen.
- INI-03: Navegar a Inicio/Respaldos y volver conserva la pestaña académica, el corte, la búsqueda, el estudiante seleccionado y las instancias de las vistas. Datos editados siguen en el servicio existente.
- INI-04: El catálogo utiliza el workspace existente y muestra su condición de demostración; catálogo vacío sin materias inventadas.
- INI-05: Respaldos comunica el estado pendiente y permite volver a Inicio. No crea archivos, modifica SQLite ni solicita cuentas.
- INI-06: Notas, asistencias, configuración y modales continúan funcionando; cambiar de sección no borra mensajes ni datos de la materia.

## Arquitectura y validación

Estado de navegación tipado en MainViewModel, separado de la última pestaña académica. HomeViewModel prepara el catálogo sin acceder a SQLite. Vistas AXAML reutilizan el diseño y la iconografía vectorial; code-behind solo carga AXAML. Mantener vistas en el árbol y alternar visibilidad.

Primero pruebas de arranque, encabezados, selección y regreso con estado; luego implementar. Compilar y ejecutar la suite. Abrir la aplicación, revisar Inicio/Mis Materias/Respaldos y regreso a Notas/Asistencias, tamaño reducido y teclado; guardar evidencias cuando sea posible. No considerar una captura headless equivalente a observar la aplicación nativa.
