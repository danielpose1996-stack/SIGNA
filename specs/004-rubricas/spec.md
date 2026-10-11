# 004 — Rúbricas por actividad

**Estado:** borrador pendiente de aprobación. **Origen:** constitución, sección III; AGENTS.md, diseño.

## Problema y objetivo

Evaluar una actividad con una rúbrica propia cuando el docente lo elija, conservando la posibilidad de usar una nota directa.

## Historias y flujo

- Como docente, quiero construir la rúbrica de una actividad en una ventana modal.
- Como docente, quiero evaluar a un estudiante con los criterios de esa rúbrica.

Flujo propuesto: actividad → abrir editor modal → configurar criterios según escala aprobada → guardar → evaluar estudiante → calcular nota con conversión aprobada. Incluye el parcial como actividad individual.

## Reglas y validaciones

Del paquete: rúbrica opcional e independiente para cada actividad, incluso el parcial; permitir nota directa; construcción en ventana modal. Criterios, niveles, pesos y conversión de puntaje a nota no están definidos. Una rúbrica no se asignará a un grupo de actividades como reemplazo de las rúbricas individuales.

## Criterios de aceptación propuestos

- **RUB-01:** Dada una actividad, cuando el docente elige construir su rúbrica, entonces el editor aparece en una ventana modal y permite cancelar sin cambios.
- **RUB-02:** Dadas dos actividades con rúbricas, cuando se edita una, entonces la otra mantiene sus criterios y evaluaciones.
- **RUB-03:** Dado un parcial, cuando se configura una rúbrica, entonces corresponde al parcial individual sin crear subactividades.
- **RUB-04:** Dada una actividad sin rúbrica, cuando se registra una nota directa válida, entonces la evaluación se completa sin exigir criterios de rúbrica.
- **RUB-05:** Dada la configuración y conversión aprobadas, cuando se evalúa con una rúbrica, entonces la nota coincide con los ejemplos aprobados y conserva el detalle de evaluación.
- **RUB-06:** Dada una rúbrica inválida según las reglas aprobadas, cuando se guarda, entonces se señalan los problemas y no se reemplaza la versión válida anterior.

## Estados de UI

Sin datos: actividad sin rúbrica. Cargando: apertura/guardado. Éxito: configuración y evaluación guardadas. Error: criterio afectado y conservación de la entrada. Modal con foco y recorrido por teclado; maqueta por aprobar.

## Modelo y migraciones

Conceptos propuestos: Rubrica, Criterio y EvaluacionRubrica asociados a Actividad y estudiante; niveles/puntajes dependen de D-06. La migración no debe invalidar notas directas existentes. Versionado o inmutabilidad tras calificar aún pendientes.

## Seguridad y errores

No sobrescribir una nota directa al añadir una rúbrica sin una política aprobada. Los cambios posteriores a calificar deben preservar información y ser explícitos.

## Pruebas previstas

Independencia entre actividades; cancelación del modal; parcial; nota directa; configuración inválida; conversión aprobada; persistencia del detalle. Casos de cambios posteriores a evaluar se añadirán al resolver D-06.

## Decisiones abiertas

D-06: estructura de criterios y niveles, pesos, escala, conversión y modificación posterior. Definir si nota directa y evaluación por rúbrica pueden coexistir y cuál determina la nota efectiva.
