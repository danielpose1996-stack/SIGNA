# 010 — Incremento autorizado

Estado: autorizado por «listo ejecuta lo planeado», el 2026-10-11. El plan y las aclaraciones del usuario gobiernan este incremento.

## Aceptación

- EVO-01: Crear un período año/semestre, crear una asignatura con código textual, nombre, escuela y período; conservarlos al reiniciar. Grupo opcional distingue secciones del mismo código. Duplicados exactos de código/grupo en un período se rechazan; se permite el mismo código en otro período.
- EVO-02: Producción empieza vacía, usa datos del usuario fuera del repositorio y no recibe alumnos de demostración. Catálogo e información de materias independientes. La demostración conserva su archivo anterior.
- EVO-03: Cada asignatura tiene tres cortes. Grupos nuevos con nombre libre, pesos suman 100 %, actividades con promedio simple sin pesos internos. Parcial tipado conserva nota única. Grupo vacío o nota pendiente impide un consolidado completo; no se convierte en cero.
- EVO-04: Rúbrica independiente por actividad, criterios suman 100 %, puntajes 0..5 por estudiante. Mezclar nota directa y rúbrica en el mismo grupo. Cambiar/desactivar una rúbrica conserva notas e instantáneas anteriores.
- EVO-05: Confirmar retiro de grupos/actividades y semanas; registros conservados en papelera local, excluidos de resultados activos. Recuperación en el mismo contexto, sin colisiones. Grupo mantiene su porcentaje al agregar/retirar una actividad.
- EVO-06: Semana de clase o actividades, P/A/J/T para ambas, nueve semanas activas en total por corte. Identificador numérico monotónico sin reutilizar estados retirados. Restaurar la décima se rechaza. Sin fechas requeridas. Porcentajes J/T pendientes.
- EVO-07: Compactar cabecera, grupos y semanas; tabla predominante con encabezado fijo y 34 px de fila. Detalle plegable conserva selección. Comparar espacio disponible con el incremento 009; comprobar listas largas y resoluciones reducidas.

## Decisiones de implementación

SQLite catálogo relacional (períodos, asignaturas, claves foráneas y unicidad). Cada materia conserva una instantánea académica versionada y validada dentro de SQLite, con identificadores estables de cortes/estudiantes/actividades/semanas; transacción y revisión optimista evitan sobreescrituras desde instancias concurrentes. No se normaliza todavía cada nota en tablas independientes: se reutilizan los modelos académicos y las instantáneas de rúbrica para minimizar pérdida y conservar atomicidad. El esquema tiene versión propia y se rechazan bases incompatibles.

Grupos heredados de la demostración conservan agregación ponderada mediante discriminador explícito. Grupos nuevos usan promedio simple. No aplicar porcentajes aproximados 33/33/34. La papelera mantiene notas y estados con el identificador original; no requiere la implementación de backups ni ofrece borrado definitivo.

Agregar alumnos reales/importar Excel continúa fuera de este incremento, como se declaró en el plan: las materias nuevas presentan estado vacío claro. Las pruebas usan alumnos ficticios en almacenamiento temporal. No afirmar que el módulo de estudiantes está implementado.

Validación: pruebas antes de producción para promedio/aislamiento/retirada/restauración; integración SQLite con reapertura, conflicto e incompatibilidad; pruebas de navegación y ventanas; build/test y revisión visual nativa cuando Computer Use esté disponible. No publicar datos de usuario en evidencias.
