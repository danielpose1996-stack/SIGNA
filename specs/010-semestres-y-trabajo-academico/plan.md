# 010 — Semestres, materias y trabajo académico

Fecha: 2026-10-11. Estado: planificación para revisión, sin autorización de implementación todavía. El usuario pidió planear estos puntos antes de ejecutarlos.

## Requisitos recibidos

1. Un docente utilizará el programa durante años. Debe crear un semestre antes de crear sus materias. La materia pide código de asignatura que puede contener números o letras, semestre, escuela y nombre de asignatura.
2. La lista de estudiantes debe tener prioridad visual en Notas y Asistencias. Las imágenes originales eran ejemplos; su distribución puede ajustarse para ganar espacio de trabajo.
3. El docente acuerda los grupos de evaluación y sus porcentajes, por ejemplo Talleres 20 %, Quices 20 % y Parcial 60 %, que deben sumar 100 % del corte. Los nombres son libres. Dentro de cada grupo se agregan actividades individuales; sus notas se promedian aritméticamente y ese promedio recibe el porcentaje del grupo. Cada actividad individual tiene una rúbrica opcional o una nota directa. La rúbrica contiene criterios ponderados y cada estudiante recibe una nota por criterio.
4. Se pueden eliminar actividades y semanas creadas por error. Al crear una semana se elige Semana de clase o Semana de actividades, esta última para períodos sin clase presencial pero con actividad.

Continúan vigentes: uso directo sin autenticación, almacenamiento local, .NET 10/Avalonia/MVVM/SQLite, tres cortes, notas de 0 a 5, rúbricas independientes, semanas añadidas manualmente y máximo nueve semanas activas por corte. No añadir fechas obligatorias ni inferir equivalencias de justificada/tarde.

## Decisiones confirmadas

- SEM, confirmado por el usuario: semestre es el período académico, por ejemplo 2026-1 o 2026-2. No agregar un nivel de carrera obligatorio.
- EVA, aclarado explícitamente por el usuario: grupos con nombre libre y porcentaje acordado, suma obligatoria 100 % por corte; promedio simple de las actividades individuales de cada grupo. No solicitar pesos adicionales para los talleres/quices individuales ni asignarles un porcentaje directo del corte. Rúbrica opcional independiente por actividad.
- ASI, confirmado por el usuario: una semana de actividades usa P/A/J/T igual que una semana de clase y se incluye en asistencia. Mantener tipo explícito para el detalle, con el mismo tratamiento de conteos y oportunidades de asistencia. La equivalencia concreta de J/T y el tratamiento porcentual de pendientes siguen pendientes de la política general.

Estas decisiones proceden de respuestas y aclaraciones explícitas del usuario en esta conversación. La fórmula de EVA ya quedó definida. Siguen abiertas las reglas generales de aprobación/redondeo, los pesos finales de los tres cortes y las equivalencias de J/T; no bloquean el diseño de este incremento si sus resultados dependientes se mantienen pendientes.

## 1. Historial por semestre y materia

Flujo propuesto: Mis Materias → crear/seleccionar semestre → crear asignatura → abrir asignatura → seleccionar corte → Notas/Asistencias.

Mis Materias tendrá un selector visible de semestre y acceso al historial. Los semestres anteriores se conservarán al crear uno nuevo. Inicio ofrecerá accesos a las materias disponibles del contexto seleccionado. No duplicar datos al cambiar de semestre y no cargar una materia ajena por defecto.

Formulario de asignatura:

| Campo | Tratamiento |
| --- | --- |
| Código | Texto obligatorio; aceptar códigos numéricos, alfabéticos y mixtos. Conservar ceros iniciales, por ejemplo 0012 o INF-301. No convertir a entero. |
| Semestre | Relación obligatoria con un semestre existente; preseleccionado desde el catálogo y visible en el formulario. |
| Escuela | Nombre obligatorio proporcionado por el docente; no imponer un catálogo institucional inventado. |
| Nombre de asignatura | Texto obligatorio, con validación cercana al campo. |

Propuesta adicional por revisar: evitar duplicados del mismo código dentro de un semestre y permitir repetirlo en semestres distintos. Si el docente necesita varios grupos de una asignatura durante el mismo semestre, añadir explícitamente el grupo a la identidad; no bloquearlo con una restricción definitiva antes de confirmar esa necesidad. No añadir nuevos campos obligatorios distintos de los solicitados por deducción.

Las materias nuevas comenzarán sin estudiantes ni notas ficticias y con sus tres cortes vacíos. Su alta no implica que el módulo de importación de estudiantes ya esté terminado: la incorporación de estudiantes reales debe resolverse con el módulo 002 antes de calificar una lista productiva.

## 2. Almacenamiento para uso durante años

El adaptador actual almacena un único demo_workspace como JSON y solo admite IsDemo=true. Esta limitación no se soluciona agregando campos al formulario.

Propuesta técnica: persistencia SQLite productiva versionada, con relaciones identificadas de forma estable entre semestre, asignatura, contexto/grupo cuando se acuerde, matrículas, cortes, actividades, calificaciones, rúbricas y semanas. Los estudiantes pueden tener identidad compartida, pero su pertenencia, notas y asistencias se relacionan con la materia/contexto correcto.

Ubicar datos productivos en una carpeta de datos del usuario, fuera de bin/obj y del repositorio. Definir la ruta multiplataforma y el comportamiento ante varias instancias en D-13. Mantener la base de demostración aislada y sin sobrescribirla. No importar sus alumnos ficticios a producción automáticamente.

Crear contratos de catálogo y contexto en Application y adaptadores en Infrastructure. Mantener la UI sin SQL ni reglas de cálculo en code-behind. Cada cambio compuesto se guarda en transacción. Las migraciones verifican versión e integridad y fallan sin reemplazar una base incompatible. No hacer una conversión silenciosa del historial existente.

## 3. Prioridad visual de la lista de estudiantes

Reorganizar la pantalla para que la tabla sea el área central con altura flexible y ocupe la mayor parte del espacio de trabajo restante:

- Encabezado de materia compacto, sin repetir información en varias filas.
- Pestañas y selector de corte compactos y siempre accesibles.
- Sustituir las tarjetas altas de actividades por una barra compacta con Agregar actividad y Configurar evaluaciones. Crear/editar en modal o panel desplegable, sin ocupar permanentemente la tabla.
- En Asistencias, sustituir el bloque alto de semanas por una barra horizontal compacta con tipo y acceso a Agregar semana.
- Panel de detalle del estudiante plegable, conservando el seleccionado. Resumen de asistencia compacto; retirar el gran indicador vacío mientras el porcentaje no tenga una política definida.
- Mantener número, documento y nombre visibles durante el desplazamiento horizontal. Encabezados fijos, cebra y selección suave. Evitar partir nombres de columnas como Documento o Semana en múltiples líneas por falta de ancho.
- Filas legibles, aproximadamente 32–36 píxeles lógicos como punto de partida; no reducir tipografía para aparentar que caben más alumnos.
- Desplazamiento y virtualización para listas largas. No prometer que una mayoría de una lista arbitrariamente grande cabe en cualquier pantalla; mostrar tantas filas útiles como permita la resolución y ofrecer vista amplia de tabla.

Validación propuesta en ventanas de 1100×700 y en la resolución real del equipo, con 10, 40 y 100 alumnos ficticios. Medir filas visibles antes/después y comprobar que la mejora sea real. No fijar un número de filas garantizado hasta medir el espacio útil y la escala de Windows.

## 4. Actividades y rúbricas

Formulario de grupo de evaluación: nombre libre y porcentaje del corte. El docente puede llamarlo Talleres, Trabajos, Actividades, Quices u otro nombre; no son categorías predefinidas obligatorias. La suma de grupos siempre debe ser 100 % al guardar una configuración válida.

Dentro del grupo, formulario de actividad individual: nombre libre y opción Usar rúbrica, sin un segundo campo de porcentaje. Todas las actividades del grupo tienen la misma participación en su promedio. No asumir que el texto Parcial activa restricciones; el tipo Parcial, si se utiliza, conserva la regla explícita ya existente de una calificación única. No crear actividades individuales obligatorias en una asignatura nueva.

Separar dos acciones:

1. Construir la rúbrica de la actividad: definir nombres/descripciones de criterios, porcentajes y escala de valoración. Los criterios deben sumar 100 %.
2. Calificar al estudiante: capturar una nota de 0 a 5 por criterio y mostrar la nota de la actividad calculada antes de guardar. Los puntajes pertenecen al estudiante, no a una plantilla compartida para toda la clase.

Ejemplo de evaluación por rúbrica: criterio A 40 % con nota 4,0 y criterio B 60 % con nota 3,0 producen 3,4 para esa actividad. Después se promedia esa nota con las demás actividades de su grupo; el porcentaje del grupo se aplica una sola vez.

Una actividad sin rúbrica permite editar directamente la celda de la tabla. Una actividad con rúbrica permite abrir la evaluación por criterios desde esa misma celda. Conservar el valor, los puntajes y la versión de rúbrica usados. Editar o desactivar una rúbrica no borra notas existentes ni las recalcula retroactivamente sin una operación explícita.

El formulario puede contener un borrador durante la edición; no guardar como configuración válida una distribución de grupos que no sume 100 %. Mostrar cuánto falta o sobra y no redistribuir porcentajes automáticamente. Agregar un taller individual no altera el 20 % acordado para Talleres: amplía la lista utilizada para su promedio.

Fórmula confirmada: promedio de grupo = suma de notas de sus actividades / cantidad de actividades; nota de corte = suma de promedio de grupo × porcentaje de grupo / 100. Calcular con precisión decimal y sin representar tres actividades como 33 %, 33 % y 34 %. La definición de rúbrica sigue usando los porcentajes de sus criterios, no un promedio simple de criterios.

Ejemplo completo para una estudiante:

| Grupo | Notas individuales | Promedio | Porcentaje del corte | Aporte |
| --- | --- | --- | --- | --- |
| Talleres | 4,0; 3,0; 5,0 | 4,0 | 20 % | 0,8 |
| Quices | 3,0; 4,0 | 3,5 | 20 % | 0,7 |
| Parcial | 4,0 | 4,0 | 60 % | 2,4 |
| Total del corte | | | 100 % | 3,9 |

Una nota no diligenciada permanece pendiente y no se convierte en cero. Mantener pendiente el consolidado hasta completar las actividades requeridas, conforme a la regla anterior que no fue modificada en esta aclaración.

El código de demostración actual aplica pesos internos por actividad. Al implementar, modificar esta regla para los nuevos grupos de promedio simple y actualizar sus pruebas. Conservar las notas y evaluaciones existentes y mantener la demostración aislada; no reescribir sus pesos ni resultados históricos mediante una migración silenciosa. Documentar cualquier conversión explícita que se llegue a necesitar.

## 5. Eliminaciones recuperables

Agregar acción Eliminar en actividades y semanas. Mostrar confirmación con nombre, materia/corte y cantidad de notas o estados afectados. Cancelar no cambia nada.

Propuesta técnica: retirar el elemento del trabajo activo y conservar sus datos en una papelera local recuperable. Permitir recuperación desde el mismo contexto. Una eliminación física definitiva o política de retención no forma parte de este incremento propuesto.

- Actividad individual: al retirarla, quitar su columna y excluirla del promedio de su grupo; conservar notas, evaluaciones por criterio e historial para recuperarla. El porcentaje del grupo no cambia. Si quedan actividades, recalcular el promedio con las restantes; si queda vacío o falta una nota requerida, mantener pendiente el resultado. Eliminar un grupo completo requiere revisar la distribución de 100 % y conservar el mismo mecanismo de recuperación; no modificar porcentajes ajenos automáticamente.
- Semana: al retirarla, quitar su columna y excluir sus estados de los conteos activos, conservándolos para recuperarla. No mover sus registros a otra semana ni asociarlos con una semana nueva mediante números reutilizados.
- Restauración desde papelera: validar conflictos, porcentajes y máximo de nueve semanas activas antes de reactivar. Identificadores internos independientes del número o nombre mostrado.
- Fallo de persistencia: conservar el estado anterior y no informar una eliminación exitosa.

La eliminación con notas actualmente está bloqueada en WorkspaceService. Sustituir ese bloqueo solo después de implementar el flujo de confirmación y recuperación; no basta con retirar la validación.

## 6. Semanas de clase o actividades

Agregar semana abre una selección sencilla entre Semana de clase y Semana de actividades. Guardar tipo explícito, identificador estable, orden del corte y registros por estudiante. Sin fechas obligatorias ni creación automática de nueve semanas.

El tipo se muestra en el encabezado de columna y en el detalle. Una semana de actividades no equivale a Sin clase: hubo una actividad registrable. Ambos tipos permiten P/A/J/T y se incluyen en los conteos y oportunidades de asistencia bajo la misma política, según respuesta explícita del usuario. No convertir un registro sin diligenciar en ausencia. Las equivalencias de justificada/tarde y el tratamiento porcentual de pendientes siguen pendientes antes de mostrar porcentajes.

Propuesta: el límite de nueve aplica al total de semanas activas de ambos tipos en cada corte, no nueve para cada tipo. Consultar un cambio de ese límite si el usuario necesita otro comportamiento.

## Orden de implementación propuesto

| Entrega | Resultado verificable |
| --- | --- |
| A | Usar el semestre como período académico confirmado; precisar reglas mínimas de identidad y crear almacenamiento productivo, catálogo de semestres y formulario/listado de materias con separación de demostración. |
| B | Corregir distribución de Notas/Asistencias, comparar filas visibles y mantener selección/edición/navegación. |
| C | Implementar grupos de nombre libre cuyos porcentajes suman 100 %, promedio simple de actividades, rúbrica opcional por actividad, notas por criterio y captura directa según EVA confirmada. |
| D | Implementar retiro y recuperación de actividades/semanas; incorporar los dos tipos de semana con P/A/J/T y su inclusión conjunta en asistencia, ya confirmada. |

Cada entrega tendrá pruebas primero, revisión de dependencias si hiciera falta, build/test y revisión visual real. Los respaldos del plan 009 continúan pendientes y reutilizarán el almacenamiento productivo; este pedido no implementa Drive ni amplía el alcance a ese módulo.

## Pruebas previstas

Semestre obligatorio; alta y reapertura durante distintos años; código numérico/alfabético/mixto con ceros iniciales; campos obligatorios; duplicados según política aprobada; repetir asignatura en otro semestre; notas/estudiantes/cortes aislados entre materias; transacción fallida sin registros parciales; migración incompatible conservando la base original; producción vacía sin alumnos ficticios.

Grupos con nombre libre y porcentajes insuficientes/excedidos/exactos; promedio simple de tres talleres; mezcla de actividades con y sin rúbrica en el mismo grupo; añadir/eliminar un taller mantiene el porcentaje del grupo; ejemplo completo de corte 3,9; criterios suman 100; nota por criterio fuera de rango rechazada; ejemplo de rúbrica 3,4; nota directa válida; vacío distinto de cero; grupo vacío pendiente; edición de rúbrica conservando evaluaciones anteriores; ninguna redistribución silenciosa ni pesos internos 33/33/34.

Eliminar/cancelar/recuperar actividad con y sin notas; borrado de semana con estados; fallo de guardado; identificación estable sin traslado de registros; cálculos y conteos excluyen elementos retirados; restauración respeta límite y conflictos; primera/novena/décima semana combinando tipos; ASI y justificada/tarde solo según política confirmada.

UI: más filas visibles que en la distribución actual, listado de 100 alumnos desplazable, encabezados/identificación fijos, notas realmente editables, panel plegable conservando selección, confirmaciones accesibles por teclado y captura de evidencias con datos ficticios.

## Archivos y capas afectados al implementarlo

- Domain: AcademicModels, AcademicRules, GradeCalculator y nuevos modelos de semestre/asignatura/contexto y eliminación recuperable.
- Application: contratos/casos de uso de catálogo y almacenamiento, WorkspaceService/contexto académico y comandos de actividad/semana.
- Infrastructure: nuevo almacenamiento productivo y migraciones; conservar SqliteWorkspaceStore como demostración o separarlo con nombre explícito.
- Desktop: HomeViewModel, MainViewModel, catálogo/formularios de semestres/materias, NotesView, AttendanceView, ConfigurationViewModel, RubricEditorViewModel, EvaluationViewModel y estilos/componentes compactos.
- Tests: ampliar Domain, Application, Infrastructure y Acceptance existentes. No introducir un framework nuevo sin necesidad.
- SDD: actualizar módulos 001/003/004/005/008 y constitución en el alcance confirmado antes de producción; este plan no aprueba sus decisiones abiertas por sí solo.

## Estado de este paso

Solo planificación. Se conservan los cambios locales del incremento 009. No se ejecutan nuevas modificaciones de código, migraciones, eliminaciones, creación de datos ni publicación en GitHub en esta solicitud de plan.
