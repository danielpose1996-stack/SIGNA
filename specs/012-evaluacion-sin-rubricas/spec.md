# 012 — Actividades con nota directa o subactividades

Autorizado por el docente: retirar completamente las rúbricas y permitir ambas modalidades en cualquier actividad, sin reglas basadas en su nombre. Sustituye las reglas de rúbricas, parcial único y ponderaciones internas anteriores.

## Modelo y flujo

EvaluationGroup representa una actividad principal del corte (nombre libre y porcentaje). EvaluationActivity representa una subactividad sin porcentaje. Modalidades explícitas: Direct (una nota por estudiante, identificada por el ID de la actividad principal) y SubActivities (subactividades editables con promedio simple). Actividad nueva: nota directa por defecto; activar subactividades es opcional. No hay criterios, escalas, evaluación por rúbrica ni restricciones particulares para Parcial.

Crear actividad → nombre y porcentaje → opcionalmente habilitar y nombrar subactividades → guardar → ingresar notas 0,0–5,0. Porcentajes principales: exactamente 100 %. Una modalidad con subactividades vacías o alguna nota pendiente no inventa ceros ni promedios parciales definitivos.

Nota del corte = suma(resultado de cada actividad × porcentaje / 100). Ejemplo: Talleres 20 % con 4, 3 y 5 = 4; Quices 20 % con 3 y 4 = 3,5; Parcial directo 60 % con 4 = 4. Resultado: 3,9. Los pesos de los tres cortes para la nota final del período siguen pendientes; no inventarlos.

## Conservación y migración

Workspace versión 2. Al abrir versión 1, conservar estudiantes, IDs, notas numéricas (incluidas las ya calculadas anteriormente), semanas, estados y retirados. Los grupos con actividades existentes pasan a subactividades, preservando nombres e IDs, incluidos grupos con una sola actividad. Se eliminan campos de rúbricas y pesos internos; todos los promedios pasan a simples. Persistir migración atómica, rechazar versiones futuras y no editar bases reales durante pruebas.

Directa → subactividades: copiar la nota directa a la primera subactividad nueva, sin borrar el registro directo; las demás quedan pendientes. Subactividades → directa: conservar subactividades retiradas y notas; asignar como nota directa el promedio completo por estudiante. Si está incompleto, dejar nueva nota pendiente y conservar registros. La interfaz advierte y solicita confirmación al retirar subactividades con el cambio. Recuperar una subactividad desde modalidad directa crea antes una primera subactividad con la nota directa actual, evitando reutilizar silenciosamente valores antiguos.

## Aceptación y TDD

- EVA-01: 20/20/60 produce 3,9 con promedio simple y nota directa; nombres libres no cambian el cálculo.
- EVA-02: nota directa sin subactividades; Parcial también admite múltiples subactividades.
- EVA-03: porcentajes distintos de 100 %, pesos fuera de rango, IDs duplicados y notas fuera de 0–5 se rechazan sin pérdida.
- EVA-04: cero válido; notas ausentes y subactividades vacías dejan pendiente; cortes independientes.
- EVA-05: migración conserva notas/estudiantes/retirados/asistencias; fallo no cambia original; reapertura SQLite mantiene modalidades y notas.
- EVA-06: cambios de modalidad y recuperación conservan registros; eliminaciones confirmadas y recuperables.
- EVA-07: no hay menú, botones, clases, servicios, ViewModels ni ventanas de rúbricas en código activo; todas las notas son editables.
- EVA-08: tabla predominante con cebra/desplazamiento y encabezados legibles; configuración de principales, porcentajes y subactividades opcionales, sin pesos internos.

Archivos: Domain, Application, SQLite, Desktop, pruebas y documentación. Sin nuevas dependencias, cambios al importador o políticas de asistencia. Revisión visual manual por solicitud del docente; compilar, probar y verificar arranque sin controlar su interfaz.
