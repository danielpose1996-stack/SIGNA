# Plan de desarrollo por incrementos

**Estado:** propuesta; ninguna etapa funcional implementada.

## Preparación documental entregada

- Archivos originales extraídos sin modificaciones.
- Siete especificaciones en borrador con escenarios de aceptación identificados.
- Carpetas de cuatro capas y cuatro grupos de pruebas.
- Arquitectura, decisiones abiertas y este plan de trabajo.

## Secuencia propuesta

| Etapa | Entregable | Condición para empezar |
| --- | --- | --- |
| 0. Revisión SDD | Constitución y arquitectura aprobadas; criterios del primer módulo confirmados | Revisión del docente |
| 1. Base técnica | Solución .NET, proyectos, shell Avalonia y herramienta de pruebas | SDK preparado y dependencias auditadas |
| 2. Materias y grupos | Crear, consultar y conservar materias/grupos localmente | Especificación 001 y maqueta aprobadas |
| 3. Importación | Vista previa XLS/XLSX y confirmación sin pérdida de datos | Identidad, campos y duplicados definidos en 002 |
| 4. Hoja de notas | Tres cortes, grupos de evaluación, actividades y notas directas | Reglas relevantes de 003 aprobadas |
| 5. Rúbricas | Editor modal y evaluación opcional por actividad | Escalas y cambios definidos en 004 |
| 6. Asistencia | Registro flexible, máximo nueve semanas por corte | Sesiones y políticas definidas en 005 |
| 7. Informes | PDF individual de asistencia | Fórmula aprobada y datos del informe definidos en 006 |
| 8. Respaldo local | Copia consistente cifrada y restauración validada | Claves, formato y compatibilidad definidos en 007 |
| 9. Drive opcional | Subida manual a carpeta elegida por OAuth | Respaldo local validado, permisos y política de datos aprobados |

La numeración de las especificaciones identifica módulos; no obliga a impedir trabajo independiente. Asistencia puede adelantarse si sus decisiones están resueltas. El respaldo local debe estar validado antes de utilizar la aplicación con datos reales, aunque se construya inicialmente con datos ficticios.

## Flujo para cada módulo

1. Resolver sus decisiones abiertas y registrar quién aprueba y cuándo.
2. Aprobar escenarios, reglas, maqueta y alcance del incremento.
3. Crear `plan.md` con archivos, modelo, migraciones y paquetes concretos; crear `tasks.md` con tareas verificables. Ambos se redactarán con la especificación aprobada, no con supuestos.
4. Escribir primero pruebas de cálculos, persistencia, importación y restauración según su riesgo; después implementar.
5. Ejecutar compilación y pruebas, revisar la interfaz y documentar resultados y limitaciones.

## Archivos del primer incremento técnico

Tras la revisión y la preparación del SDK se propone crear `GestorDocente.slnx`, los `.csproj` bajo `src/` y `tests/`, `global.json` con la versión de SDK elegida y una configuración común de compilación. La política concreta de fijación de paquetes se registrará antes de restaurarlos.

No se han generado estos archivos porque el paquete solicita aprobar la constitución y el alcance antes de crear la solución.

## Casos de prueba prioritarios

| Riesgo | Verificación prevista |
| --- | --- |
| Persistencia incompleta | Reinicio, relaciones inválidas y fallo transaccional |
| Importación incorrecta | XLS/XLSX inválido, duplicados, cancelación y fallo antes de confirmar |
| Notas erróneas | Límites de escala, pesos y precisión conforme a las reglas aprobadas |
| Rúbricas inconsistentes | Independencia por actividad y cambios posteriores a calificar |
| Asistencia incorrecta | Novena/décima semana, períodos sin clase y política de estados |
| Pérdida en restauración | Archivo corrupto, clave incorrecta y versión incompatible |

## Criterios de cierre de un incremento

- Escenarios aprobados enlazados a evidencia de pruebas.
- `dotnet build` y `dotnet test` ejecutados sobre la solución, con resultados registrados.
- Revisión visual en Windows: tamaño normal, redimensionado, escala de pantalla, teclado y estados sin datos/error.
- Validación en macOS antes de declarar soporte de ese sistema.
- Migraciones y recuperación verificadas cuando corresponda.
- Especificación, decisiones y documentación actualizadas.

No hay resultados de compilación o tests en esta preparación: aún no existen proyectos compilables y no se detectó `dotnet` en el PATH.

## Verificación de la estructura entregada

El 2026-10-11 se compararon por SHA-256 los siete archivos extraídos con sus originales en el ZIP: todos coinciden. Se comprobó que existen siete `spec.md`, cada uno con los nueve apartados requeridos. El repositorio local se inicializó en `main`; todos los archivos están sin commit y no se configuró un remoto.
