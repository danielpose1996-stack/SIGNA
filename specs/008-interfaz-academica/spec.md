# 008 — Primera implementación de interfaz académica

Estado: alcance autorizado por la solicitud «IMPLEMENTACIÓN 01» del 2026-10-11. No implica aprobación de reglas ajenas a este incremento. Referencias visuales: `design-references/notas-rubrica.png` y `design-references/asistencias.png`.

## Reglas recibidas del usuario

- C#/.NET 10, Avalonia, AXAML, Fluent y MVVM. Respetar las cuatro capas existentes.
- Tres cortes independientes, sin reconstruir ventanas al navegar.
- Notas entre 0 y 5 inclusive; porcentajes de grupos suman 100 y los de sus actividades suman 100. Promedio ponderado: suma de nota × peso/100 por actividad, luego suma de resultado de grupo × peso/100.
- El grupo de tipo Parcial contiene exactamente una actividad de peso interno 100.
- Cada actividad tiene una rúbrica opcional. Criterios con suma 100; niveles editables sin solapamientos o huecos. Editor modal con borrador descartable.
- Capturar una nota vacía como pendiente; no transformarla en cero. Consolidado pendiente hasta que todas las actividades estén calificadas. Formato de dos decimales solo para visualización; conservar precisión decimal en cálculos, sin aplicar reglas de aprobación ni nota final de tres cortes.
- Evaluación por rúbrica: puntaje de criterio entre 0 y 5, promedio ponderado de criterios para obtener nota de actividad. Se conserva la rúbrica usada y sus puntajes en cada evaluación; modificar su definición no borra ni recalcula notas anteriores. Esta convención técnica de captura usa la misma escala solicitada y queda documentada para revisión.
- Niveles sobre porcentaje del puntaje: intervalos [inferior, superior), último intervalo incluye 100; deben cubrir [0,100] sin huecos. Ejemplo editable: 0–50, 50–70, 70–90, 90–100. No son umbrales institucionales de aprobación.
- Máximo nueve semanas añadidas manualmente por corte, sin fechas ni rangos de días. Estado vacío pendiente; P/A/J/T editables. Omitir períodos sin clase; también reconocer registros heredados sin clase sin contabilizarlos.
- Porcentaje y clasificación de asistencia pendientes de política para J/T y registros no diligenciados. Mostrar conteos, no un porcentaje ficticio.
- PDF, Drive, respaldos, importación y CRUD de materias no pertenecen a este incremento. Acciones futuras deshabilitadas con motivo visible.

## Modelo y almacenamiento

Workspace de demostración con estudiantes ficticios y tres cortes; configuración, notas, rúbricas y semanas por corte. Contrato de almacenamiento en Application; adaptador SQLite en Infrastructure para una instantánea JSON versionada y transacción atómica. Base exclusiva de demostración en `.demo-data/`, sin aceptar datos reales ni migrarla a producción. Un fallo de guardado no confirma el cambio en memoria. Esta persistencia mínima no constituye el esquema relacional productivo de los módulos futuros.

## Criterios de aceptación

- UI-01: Dadas las referencias, al abrir la ventana se muestran menú UNIPAZ, encabezado, pestañas y selección de corte coherentes. Sin recurso oficial separado, se usa el nombre institucional sin inventar un símbolo.
- UI-02: Dado cualquier corte, al editar una nota válida se recalculan grupo/corte y se conserva en SQLite; una nota fuera de rango se rechaza sin modificar el dato anterior.
- UI-03: Dadas actividades ponderadas, sus resultados respetan sumas 100 y el parcial único; una configuración inválida impide guardar.
- UI-04: Dada una actividad, al abrir el modal se edita una copia; cancelar descarta cambios y guardar válido afecta solo a esa actividad. El fondo permanece bloqueado y atenuado.
- UI-05: Dados criterios y niveles inválidos, guardar muestra el error y conserva el modal abierto. Se pueden añadir, editar, eliminar con confirmación y previsualizar.
- UI-06: Dada una actividad con rúbrica, evaluar por criterios genera una nota ponderada y conserva su detalle; editar la definición no elimina notas existentes.
- UI-07: Dado un corte, agregar semana añade solo una; tras nueve se impide la décima. Estados y semanas no se trasladan a otro corte.
- UI-08: Dada una selección de estudiante, el panel refleja documento, conteos y estado por semana; vacíos y sin clase no cuentan como ausentes. Porcentajes pendientes se identifican.
- UI-09: Dadas muchas filas/columnas, las tablas son editables, usan cebra, encabezados fijos, selección visible y desplazamiento. Buscar filtra por nombre/documento.
- UI-10: Dada una ventana reducida o escala mayor, hay desplazamiento donde hace falta y el modal cabe en pantalla; botones y campos accesibles por teclado.
- UI-11: Dado el flujo PDF pendiente, el botón lo indica y no genera archivos ni éxito simulado.

## Pruebas y decisiones abiertas

Pruebas de dominio: escala, ponderación, parcial, porcentajes, intervalos, semanas, conteos. Aplicación/presentación: cortes, independencia, guardado, selección, borradores. Integración: round trip SQLite y conservación tras fallos. Ventanas: prueba de apertura/cierre modal con Avalonia Headless si se audita el paquete. Revisión visual real Windows y capturas de notas/modal/asistencia, redimensionado y escala.

Pendientes: política de asistencia; pesos finales de cortes; reglas de aprobación y redondeo institucional; esquema productivo; logo oficial; PDF; validación macOS. Las especificaciones 003–005 siguen siendo propuestas para el producto completo; este documento gobierna únicamente el incremento autorizado.
