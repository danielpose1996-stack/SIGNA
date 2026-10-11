# 009 — Inicio, uso directo y acceso a respaldos

Fecha: 2026-10-11. Estado: plan propuesto para revisión; no implementado.

## Solicitud confirmada

El programa se abre listo para usar, sin registro, inicio de sesión ni cuenta obligatoria. El menú tendrá un apartado de respaldos. El docente podrá guardar un archivo local para conservarlo o subirlo por su cuenta a Drive; también podrá conectar opcionalmente su cuenta de Google para enviar respaldos desde el programa. Esta autorización externa solo se solicitará al elegir conectar Drive.

El usuario pidió planificar este paso y explicarlo antes de implementar. No se modifica código ni se considera aprobado el diseño propuesto por el hecho de documentarlo.

## Situación actual comprobada

- App.axaml.cs abre MainWindow directamente; no hay autenticación de la aplicación.
- MainWindow.axaml muestra Inicio deshabilitado; MainViewModel inicia en Notas.
- La selección de Mis Materias está fija visualmente; debe reflejar la sección realmente abierta.
- El shell incluye encabezado y pestañas de una materia incluso fuera de un módulo global; se deben mostrar solo en el contexto correspondiente.
- La persistencia SQLite actual acepta exclusivamente el workspace de demostración. No existe todavía gestión productiva de varias materias, almacenamiento definitivo, backup ni restauración.
- El módulo 007 ya contempla respaldo consistente, cifrado y Drive opcional. Formato, claves y recuperación siguen pendientes.

## Propuesta para Inicio

Una página de trabajo sencilla con el mismo menú, colores, tipografía e iconografía de las vistas académicas:

- Título Inicio y texto breve que indique que los datos se guardan en este equipo.
- Lista de materias recientes o disponibles con nombre, grupo y período, y acceso a su pantalla académica. Mostrar únicamente información disponible; no inventar indicadores.
- Accesos a Mis Materias y Respaldos. Crear materia e importar estudiantes se habilitarán cuando sus módulos funcionen.
- Estado del último respaldo confirmado: fecha y destino; si no existe, mostrar Todavía no has creado un respaldo.
- Sin gráficos decorativos, notificaciones ficticias ni estadísticas cuya política académica no esté definida.
- En el primer uso sin materias, mostrar una explicación clara y las acciones realmente implementadas. Mientras solo exista la demostración, identificarla explícitamente.
- El nombre del docente podrá configurarse localmente en un incremento posterior; no representa una cuenta ni requiere correo. No presentar Juan Pérez como identidad real del usuario.

## Propuesta para el apartado Respaldos

Ubicación: menú lateral, junto a Reportes y antes de Configuración. Etiqueta visible: Respaldos.

| Opción | Comportamiento propuesto |
| --- | --- |
| Crear respaldo local | Elegir destino, crear una copia completa y verificable, guardar con nombre único y mostrar ruta/fecha cuando termine correctamente. El docente puede subir ese mismo archivo a Drive manualmente. |
| Restaurar respaldo | Elegir archivo, verificarlo, mostrar qué se reemplazará, pedir confirmación y preservar una copia recuperable del estado anterior. |
| Google Drive opcional | Mostrar Desconectado, Conectar Google Drive y, tras autorizar, cuenta/destino, Enviar respaldo y Desconectar. No iniciar subidas automáticas. |

La conexión a Drive usa la autorización oficial de Google en el navegador del sistema; no se pide la contraseña de Gmail dentro de Avalonia. No se solicita acceso al correo. Los permisos concretos, la selección de carpeta y la configuración del cliente OAuth para distribuir la aplicación se definirán en la entrega de Drive.

Los respaldos deben incluir los datos y ajustes locales necesarios para recuperar el trabajo, con versión del formato y compatibilidad verificable. Excluir credenciales Google, tokens, SDK, ejecutables temporales y logs. El inventario definitivo se fijará al definir el almacenamiento productivo.

Propuesta de cifrado pendiente de revisión técnica: archivo portable protegido mediante una contraseña de respaldo, independiente del acceso al programa. Debe restaurarse en otro equipo. Definir cómo se crea/custodia la clave y explicar la recuperación disponible antes de implementar; no prometer recuperar una contraseña perdida. El archivo local también debe ser apto para subir manualmente a Drive sin exponer una base sin cifrar.

## Entregas en orden

### A — Inicio y navegación

Habilitar Inicio y convertirlo en pantalla inicial. Crear navegación con selección visible y vistas retenidas; conservar corte, notas, asistencias y búsqueda al salir y volver. Separar el encabezado global del encabezado de materia. Añadir acceso visible a Respaldos con el alcance pendiente explicado, sin botones que simulen crear archivos o conectar cuentas. Mantener Notas, Asistencias y los modales existentes.

Archivos previstos: Views/MainWindow.axaml, ViewModels/MainViewModel.cs, nuevas Views/HomeView.axaml y ViewModels/HomeViewModel.cs; extraer componentes compartidos cuando lo requiera la navegación. Pruebas en los proyectos de Application/Acceptance existentes.

### B — Respaldo local y restauración

Resolver primero ubicación y formato de datos productivos (D-13) y formato/cifrado/claves (D-09). Conservar la demostración aislada; no convertir silenciosamente su base en información real. Se pueden probar los servicios de respaldo con datos ficticios antes de habilitar su uso productivo.

Definir contratos en Application; implementar SQLite, archivos, cifrado y restauración en Infrastructure; añadir BackupView y su ViewModel en Desktop. Obtener la copia mediante la API de respaldo consistente de SQLite. Verificar en una ubicación temporal antes de reemplazar; coordinar conexiones y cambios pendientes. Restaurar de manera recuperable, con confirmación y copia de seguridad previa. No borrar notas, rúbricas o asistencias si la operación falla.

Completar este flujo antes de conectar Drive: permite trabajar sin internet y subir el archivo manualmente.

### C — Google Drive opcional

Reutilizar exactamente el archivo de respaldo local ya validado. Auditar dependencias y configurar el cliente OAuth de escritorio. Autorizar con navegador y permisos mínimos, elegir destino permitido, enviar por acción explícita y mostrar éxito solo tras confirmar la subida. Proteger tokens fuera de Git, permitir desconectar y conservar toda la funcionalidad local ante cancelación, rechazo o fallo de red. Resolver D-10 para el destino y consentimiento; no añadir sincronización ni Gmail API.

## Criterios de aceptación y pruebas previstas

- INI-01: Al abrir sin conexión y sin cuenta Google, aparece Inicio y se puede acceder a los módulos implementados.
- INI-02: La sección seleccionada coincide con la página; Inicio y Respaldos no muestran pestañas ni título de una materia ajena al contexto.
- INI-03: Ir a Inicio y volver a la materia conserva los datos, el corte y la selección; no reconstruye innecesariamente las vistas académicas.
- INI-04: Inicio usa datos existentes, contempla estado vacío y mantiene claramente separada la demostración.
- RES-09: Crear/restaurar respaldo local no exige cuenta Google ni internet; cancelar el selector no genera un archivo ni informa éxito.
- RES-10: El archivo local puede restaurarse en otro equipo compatible con el mecanismo de claves definido y también sirve para subirlo manualmente a Drive.
- RES-11: Sin copia previa confirmada no se muestra una fecha ficticia; un fallo no actualiza el último respaldo satisfactorio.
- RES-12: Desconectar o rechazar Google mantiene disponible el trabajo académico y los respaldos locales; ningún token o contraseña aparece en archivos versionados ni en el respaldo.
- RES-13: Subir a Drive requiere acción explícita; un fallo conserva el archivo local y muestra un error real.

Reutilizar además RES-01 a RES-08 del módulo 007: integridad, clave incorrecta, archivo corrupto, compatibilidad, colisión de nombres y restauración recuperable. Compilar, ejecutar pruebas relevantes y revisar visualmente las nuevas vistas en la aplicación real antes de dar cada entrega por terminada.

## Fuentes técnicas consultadas

- [OAuth para aplicaciones de escritorio de Google](https://developers.google.com/identity/protocols/oauth2/native-app): autorización en navegador, cliente de escritorio y PKCE.
- [Permisos de Google Drive](https://developers.google.com/workspace/drive/api/guides/api-specific-auth): priorizar permisos limitados; evaluar drive.file y un selector compatible en la entrega C.

## Resultado de esta planificación

Solo documentación. No se añadieron paquetes, cuentas, servicios, archivos de respaldo ni pantallas. La siguiente entrega propuesta es A; B y C tienen dependencias explícitas y se implementarán por separado.
