# 001 — Materias y grupos académicos

**Estado:** borrador pendiente de aprobación. **Origen:** constitución, sección III. Los detalles propuestos no equivalen a reglas aprobadas.

## Problema y objetivo

Organizar el trabajo del docente por materia y grupo académico, con almacenamiento local y sin depender de una conexión a internet.

## Historias y flujo

- Como docente, quiero crear una materia y consultar sus grupos.
- Como docente, quiero seleccionar un grupo para acceder a sus estudiantes, cortes, notas y asistencias.

Flujo propuesto: lista de materias → crear materia → crear grupo asociado → seleccionar grupo. Edición y borrado quedan sujetos a D-02.

## Reglas y validaciones

Del paquete: crear materias y grupos; operación local; tres cortes para cada contexto académico. Propuesta a revisar: identificación estable y relación obligatoria del grupo con su materia. No fijar campos obligatorios, unicidad ni política de borrado antes de resolver D-02.

## Criterios de aceptación propuestos

- **MAT-01:** Dado el formulario con los campos aprobados, cuando el docente guarda una materia válida, entonces aparece en la lista y se conserva al reiniciar la aplicación.
- **MAT-02:** Dada una materia existente, cuando se crea un grupo válido asociado a ella, entonces el grupo aparece dentro de esa materia y permite acceder a sus tres cortes.
- **MAT-03:** Dada una entrada que incumple una validación aprobada, cuando se intenta guardar, entonces se señala el campo y no se persisten registros incompletos.
- **MAT-04:** Dado un error de almacenamiento, cuando falla el guardado, entonces se informa al docente y se mantiene la entrada para corregir o reintentar.
- **MAT-05:** Dado el equipo sin conexión, cuando se crean y consultan materias y grupos, entonces funcionan los mismos casos de uso locales.

## Estados de UI

Sin datos: invitación a crear materia/grupo. Cargando: lectura local en curso. Éxito: lista actualizada y contexto seleccionado. Error: explicación y opción de reintento sin perder la entrada. Diseño y navegación pendientes de maqueta.

## Modelo y migraciones

Conceptos propuestos: Materia, GrupoAcademico y Corte; relaciones e identificadores por aprobar. Primera migración con restricciones de integridad acordadas. No se incluye un esquema SQL ni una migración ejecutable en este borrador.

## Seguridad y errores

No incluir datos reales en demostraciones. Guardar operaciones relacionadas de forma consistente. Los detalles técnicos del error no deberán exponer información personal.

## Pruebas previstas

Dominio: validaciones aprobadas e integridad de relaciones. Aplicación: creación y selección, errores del repositorio. Integración: persistencia tras reapertura y fallo sin registros parciales. Aceptación: MAT-01 a MAT-05 y navegación por teclado.

## Decisiones abiertas

D-02: campos, identificadores, períodos, duplicados, edición y borrado. D-11: maqueta. D-13: ubicación de datos y migraciones. Resolver estas decisiones antes de implementar el módulo.
