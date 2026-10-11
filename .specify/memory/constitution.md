# Constitución del proyecto — Gestor Académico Docente
**Versión:** 0.1.0 (propuesta inicial, pendiente de aprobación)
**Estado:** Borrador SDD

## I. Propósito
Aplicación de escritorio para que docentes administren materias, grupos, estudiantes, evaluaciones, calificaciones y asistencias, reemplazando el manejo manual de varias hojas de Excel.

## II. Principios no negociables
1. **Local-first:** lectura, escritura y cálculo offline por defecto; ningún servidor obligatorio.
2. **Multiplataforma:** Windows 10/11 y macOS como objetivos; validar builds por sistema. Usar C#/.NET 10 + Avalonia, no WPF.
3. **Integridad académica:** las notas, ponderaciones, datos importados y cambios nunca se pierden silenciosamente.
4. **Especificación antes de código:** toda funcionalidad requiere escenarios de uso, reglas y criterios de aceptación aprobados.
5. **TDD proporcional al riesgo:** cálculos, persistencia, importación y restauración deben tener tests automatizados.
6. **Interfaz académica:** accesible, sobria, consistente, sin saturación ni pasos innecesarios.
7. **Privacidad por diseño:** datos personales protegidos, sin subir información a terceros salvo acción explícita del usuario.
8. **Seguridad de la cadena de suministro:** ninguna skill, paquete NuGet, herramienta, script o MCP nuevo se ejecuta sin revisar procedencia, licencia, permisos y dependencias.
9. **Separación de responsabilidades:** UI/MVVM, dominio, aplicación, infraestructura y persistencia desacopladas.
10. **Recuperabilidad:** respaldos consistentes, cifrados y restaurables; nunca sobrescribir antes de verificar.

## III. Dominio funcional aprobado
- Crear materias y grupos académicos; importar roster XLS/XLSX con vista previa y revisión de duplicados.
- Tres cortes académicos; cada corte tiene su hoja de notas independiente.
- Docente configura actividades principales con nombre libre y porcentajes que suman 100 % por corte. Cualquier actividad admite nota directa o subactividades opcionales con promedio simple, incluido Parcial.
- Rúbricas retiradas por solicitud del docente; conservar notas numéricas existentes según `specs/012-evaluacion-sin-rubricas/spec.md`.
- Asistencias: añadir semanas según se dicte clase, máximo nueve semanas por corte; sin obligación de registrar semanas festivas.
- Estados individuales: presente, ausente, justificada, tarde; distinguir también «sin clase» (no cuenta como asistencia).
- Informe de asistencia de un estudiante a PDF, mostrando estados, períodos registrados y cálculo según política aprobada.
- Respaldo manual local y opcional en Google Drive en carpeta elegida mediante OAuth; archivos fechados, cifrados, sin sobrescribir; restauración en otro equipo.

## IV. Reglas pendientes de definir antes de programarlas
- Pesos de cada corte sobre la nota final; redondeo y reglas de aprobación.
- Cómo se computan ausencias justificadas y tardanzas en las estadísticas de asistencia.
- Agregación interna definida: promedio aritmético simple de las subactividades.
- Política de retención, consentimiento y ubicación autorizada de respaldos en cuentas personales o institucionales.
- Estrategia de claves de cifrado y recuperación de acceso a los archivos cifrados.

## V. Aceptación y gobierno de cambios
- Cada módulo tendrá su especificación en `specs/`, escenarios y pruebas asociadas.
- Cualquier cambio en reglas de negocio o arquitectura requiere actualizar la especificación antes del código.
- Se prohíben credenciales en repositorio y datos reales en fixtures o demostraciones.
- No declarar listo un módulo sin build correcto, tests ejecutados y validación visual/documentada.
- Las versiones del framework y paquetes quedarán fijadas en archivos del proyecto y actualizadas con revisión.
