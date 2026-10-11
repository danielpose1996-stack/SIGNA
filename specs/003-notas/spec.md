# 003 — Cortes, evaluaciones y notas

**Estado:** borrador pendiente de aprobación. **Origen:** constitución, secciones III y IV.

## Problema y objetivo

Gestionar tres hojas de notas independientes por grupo académico, permitiendo organizar actividades en grupos de evaluación configurados por el docente.

## Historias y flujo

- Como docente, quiero seleccionar un corte y configurar los grupos de evaluación y sus porcentajes.
- Como docente, quiero registrar notas directas por actividad y estudiante.
- Como docente, quiero consultar los resultados calculados según la política aprobada.

Flujo propuesto: grupo académico → corte → configuración de evaluaciones → actividades → tabla de notas → resultados. El parcial se registra como evaluación individual.

## Reglas y validaciones

Del paquete: exactamente tres cortes con hojas independientes; grupos de evaluación con varias actividades; parcial con una calificación única sin subactividades; rúbrica opcional por actividad. No fijar escala, pesos, redondeo, aprobación, agregación ni tratamiento de pendientes antes de D-04/D-05.

## Criterios de aceptación propuestos

- **NOT-01:** Dado un grupo académico, cuando se consulta cada uno de sus tres cortes, entonces cada hoja mantiene sus propias actividades y notas sin trasladar cambios a las otras.
- **NOT-02:** Dado un grupo de evaluación, cuando se añaden varias actividades, entonces cada una admite una nota individual por estudiante.
- **NOT-03:** Dado un parcial, cuando se configura o califica, entonces dispone de una calificación por estudiante y no permite subactividades.
- **NOT-04:** Dada una actividad sin rúbrica, cuando se introduce una nota válida, entonces se guarda directamente sin exigir crear una rúbrica.
- **NOT-05:** Dada una escala aprobada, cuando se ingresa una nota fuera de sus límites, entonces se rechaza con un mensaje y se conserva el dato anterior.
- **NOT-06:** Dados pesos, notas y política de agregación/redondeo aprobados, cuando se calcula un resultado, entonces coincide con los ejemplos numéricos incorporados a esta especificación antes de implementarlo.
- **NOT-07:** Dada una configuración de porcentajes inválida según la política aprobada, cuando se guarda o calcula, entonces se señala el problema y no se presenta un resultado válido engañoso.
- **NOT-08:** Dado un error al guardar una nota, cuando ocurre el fallo, entonces la UI identifica el cambio no guardado y permite recuperarlo o reintentarlo.

## Estados de UI

Sin datos: grupo sin estudiantes o corte sin actividades. Cargando: lectura o guardado local. Éxito: notas y estado de guardado visibles. Error: celda/configuración afectada y reintento. Tabla con cebra y navegación por teclado, según maqueta aprobada.

## Modelo y migraciones

Conceptos propuestos: Corte, GrupoEvaluacion, Actividad y Calificacion vinculada a matrícula y actividad. El tipo de parcial debe impedir subactividades. Restricciones y representación de valores/precisión por definir; migraciones conservarán las notas existentes.

## Seguridad y errores

Evitar pérdida silenciosa, distinguir dato editado de dato guardado y validar también fuera de la UI. Política de cambios en pesos, actividades o escala con notas existentes pendiente; no recalcular ni eliminar información silenciosamente.

## Pruebas previstas

Independencia de cortes; varias actividades; parcial sin subactividades; notas directas; bordes de escala; nota vacía/cero; pesos y precisión; edición y fallo de persistencia. NOT-06 requiere ejemplos numéricos aprobados, todavía ausentes.

## Decisiones abiertas

D-04/D-05: escala, pesos por corte, aprobación, suma de porcentajes, promedio simple/ponderado, pendientes y redondeo. Definir cuándo se guarda una celda y qué ocurre al cambiar evaluaciones ya calificadas. Estos criterios no habilitan todavía el desarrollo de cálculos.
