# Decisiones pendientes

**Estado:** abiertas; no representan valores predeterminados. Las propuestas añadidas durante esta estructuración requieren revisión del docente.

| ID | Decisión | Módulos afectados | Momento de resolver |
| --- | --- | --- | --- |
| D-01 | Aprobar o ajustar la constitución original y la arquitectura propuesta | Todos | Antes de crear la solución |
| D-02 | Campos de materia/grupo, identificación, duplicados, período académico y política de edición/borrado | 001 | Antes de implementar 001 |
| D-03 | Campos y clave de identidad del estudiante, columnas Excel, hojas y resolución de duplicados | 002 | Antes de implementar importación |
| D-04 | Escala de notas; significado de nota vacía y cero; pesos de cortes y aprobación | 003 | Antes de cálculos y captura dependientes |
| D-05 | Validación de porcentajes, promedio simple/ponderado, actividades pendientes y redondeo | 003 | Antes de implementar agregaciones |
| D-06 | Escala y estructura de rúbricas; conversión a nota; cambios tras calificar | 004 | Antes de implementar rúbricas |
| D-07 | Cantidad de sesiones por semana; justificada/tarde; porcentaje y denominador de asistencia | 005, 006 | Antes de cálculos e informes |
| D-08 | Datos incluidos en PDF, períodos, fórmula visible y tratamiento de registros incompletos | 006 | Antes de generar el informe |
| D-09 | Cifrado, custodia de claves, recuperación de acceso y formato/versiones de respaldo | 007 | Antes de respaldo y restauración |
| D-10 | Retención, consentimiento y carpetas/cuentas autorizadas para Drive | 007 | Antes de integrar Drive |
| D-11 | Maquetas, navegación y revisión de accesibilidad por módulo | Desktop | Antes de implementar la UI afectada |
| D-12 | Versiones, licencias y dependencias; SDK, Avalonia, SQLite, pruebas, Excel y PDF | Base técnica | Auditar cada componente antes de instalarlo |
| D-13 | Ubicación local de datos, soporte de varias instancias y estrategia de migración | Persistencia | Antes de escribir persistencia |

## Primera revisión sugerida

Para empezar con materias y grupos bastará resolver D-01, D-02, D-11 para ese módulo, D-12 para la base técnica y D-13. Las demás decisiones pueden resolverse con sus módulos.

## Registro de aprobación

Cuando el docente decida, actualizar la especificación afectada y registrar fecha, decisión y alcance. No cambiar una especificación a «aprobada» por el hecho de crear esta estructura. Las decisiones arquitectónicas se registrarán en `docs/decisions/`; las reglas académicas seguirán en las especificaciones y en la constitución cuando corresponda.
