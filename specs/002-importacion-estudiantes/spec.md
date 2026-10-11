# 002 — Importación de estudiantes

**Estado:** borrador pendiente de aprobación. **Origen:** constitución, sección III.

## Problema y objetivo

Incorporar un listado de estudiantes desde XLS/XLSX a un grupo académico, revisando el contenido y los duplicados antes de guardar.

## Historias y flujo

- Como docente, quiero revisar una vista previa de la hoja de estudiantes.
- Como docente, quiero resolver los duplicados antes de confirmar la importación.

Flujo propuesto: seleccionar grupo → elegir archivo y hoja → asignar columnas cuando corresponda → validar → revisar vista previa y duplicados → confirmar → resultado. La estrategia de asignación de columnas está pendiente.

## Reglas y validaciones

Del paquete: soportar XLS y XLSX; vista previa; revisión de duplicados; no perder datos silenciosamente. Propuestas a revisar: no modificar el grupo durante la vista previa; confirmación atómica. Los campos, la clave de identidad, la estrategia para registros inválidos y las acciones ante duplicados se definirán en D-03.

## Criterios de aceptación propuestos

- **IMP-01:** Dado un XLS o XLSX válido con la estructura aprobada, cuando se abre, entonces se muestran las filas y su validación antes de modificar el grupo.
- **IMP-02:** Dada una coincidencia según la clave de identidad aprobada, cuando se prepara la importación, entonces se muestra el duplicado dentro del archivo o frente al grupo y se aplica únicamente la resolución explícita permitida.
- **IMP-03:** Dada una vista previa, cuando el docente cancela, entonces el grupo conserva sus datos anteriores.
- **IMP-04:** Dada una selección validada, cuando se confirma, entonces se guardan los registros elegidos y se informa cuántos se incorporaron y cuáles no.
- **IMP-05:** Dado un archivo corrupto, incompatible o con campos requeridos ausentes, cuando se lee, entonces se informa el problema sin modificar los datos existentes.
- **IMP-06:** Dado un fallo de persistencia durante la confirmación, cuando la operación termina, entonces el grupo no queda con una importación parcial y se permite reintentar.

## Estados de UI

Sin datos: seleccionar archivo. Cargando: lectura y validación, evitando doble confirmación. Éxito: resumen del resultado. Error: fila/columna cuando sea posible, con opción de corregir o elegir otro archivo. Vista previa con duplicados identificables por texto además del color.

## Modelo y migraciones

Conceptos propuestos: Estudiante y Matricula, con relación a GrupoAcademico. La vista previa usa datos temporales hasta confirmar. Migración y unicidad dependen de la clave de identidad aprobada.

## Seguridad y errores

Leer datos del archivo sin ejecutar macros ni fórmulas. No subir el archivo a terceros. Auditar el lector y definir límites de tamaño/filas. Evitar registrar datos personales de filas completas en logs.

## Pruebas previstas

Fixtures ficticios XLS y XLSX; hojas múltiples; columnas faltantes; duplicados internos y existentes; archivo inválido; cancelación; fallo transaccional; reimportación conforme a la política aprobada. Trazabilidad IMP-01 a IMP-06.

## Decisiones abiertas

D-03: columnas, campos, identidad, selección de hojas, duplicados y filas inválidas. D-12: lector Excel y límites de recursos. Decidir si se admitirán fórmulas, valores calculados almacenados o solamente celdas con valores directos.
