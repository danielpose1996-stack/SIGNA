# 005 — Registro de asistencias

**Estado:** borrador pendiente de aprobación. **Origen:** constitución, secciones III y IV.

## Problema y objetivo

Registrar asistencia según las semanas en que se dicte clase, sin obligar al docente a llenar semanas festivas.

## Historias y flujo

- Como docente, quiero añadir semanas de clase a un corte cuando sea necesario.
- Como docente, quiero registrar presente, ausente, justificada o tarde por estudiante.
- Como docente, quiero distinguir un período sin clase de una ausencia.

Flujo propuesto: grupo → corte → añadir semana/período → registrar estados → guardar → consultar historial. La cantidad de sesiones por semana está pendiente.

## Reglas y validaciones

Del paquete: máximo nueve semanas por corte; no exigir semanas festivas; estados presente, ausente, justificada y tarde; «sin clase» no cuenta en el cálculo de asistencia. No asignar equivalencias o penalizaciones a justificada/tarde. La diferencia entre pendiente y ausencia debe definirse expresamente.

## Criterios de aceptación propuestos

- **ASI-01:** Dado un corte con menos de nueve semanas, cuando se añade una semana válida, entonces puede registrarse sin exigir completar semanas anteriores o festivas.
- **ASI-02:** Dado un corte con nueve semanas registradas, cuando se intenta añadir una décima, entonces se informa el límite y no se crea.
- **ASI-03:** Dado un estudiante y un período de clase, cuando se registra uno de los cuatro estados individuales, entonces se conserva ese estado al reabrir el corte.
- **ASI-04:** Dado un período marcado sin clase, cuando se calcula asistencia con una política aprobada, entonces no aumenta el denominador de oportunidades de asistencia ni se cuenta como ausencia.
- **ASI-05:** Dados registros de justificada y tarde, cuando se calculan estadísticas, entonces se aplica exclusivamente la política aprobada, con ejemplos numéricos añadidos antes de implementarla.
- **ASI-06:** Dado un error durante el guardado, cuando falla la operación, entonces se identifica qué no se guardó y no se comunica éxito.

## Estados de UI

Sin datos: ninguna semana registrada. Cargando: lectura/guardado. Éxito: estados guardados. Error: período/estudiante afectado y reintento. Los estados se distinguen por etiquetas además del color.

## Modelo y migraciones

Conceptos propuestos: SemanaCorte, posible SesionClase y RegistroAsistencia relacionado con matrícula. «Sin clase» se propone como condición del período, pendiente de aprobación del modelo. Restricción de nueve semanas independiente del número de sesiones que se acuerde.

## Seguridad y errores

Conservar los estados históricos. Los cambios de fecha, la eliminación de una semana y la modificación de períodos ya registrados requieren una política explícita. No convertir registros vacíos en ausencias automáticamente.

## Pruebas previstas

Primera/novena/décima semana; cortes independientes; festivos omitidos; todos los estados; sin clase; sin registros; persistencia y fallo de guardado. Estadísticas pendientes de ejemplos aprobados.

## Decisiones abiertas

D-07: sesiones por semana, estados pendientes, justificaciones, tardanzas, fórmula y denominador. Definir tratamiento de estudiantes incorporados a mitad del corte y edición/borrado de períodos.
