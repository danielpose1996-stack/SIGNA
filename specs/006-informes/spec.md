# 006 — Informe individual de asistencia en PDF

**Estado:** borrador pendiente de aprobación. **Origen:** constitución, sección III.

## Problema y objetivo

Generar un informe de asistencia de un estudiante que permita revisar los estados, los períodos registrados y el cálculo según la política aprobada.

## Historias y flujo

- Como docente, quiero seleccionar a un estudiante y exportar su informe de asistencia.
- Como docente, quiero entender qué registros sustentan las estadísticas del informe.

Flujo propuesto: seleccionar grupo y estudiante → elegir alcance aprobado → revisar → elegir destino local → generar PDF. Alcance y diseño del informe pendientes.

## Reglas y validaciones

Del paquete: informe individual a PDF con estados, períodos y cálculo según política aprobada. Depende del módulo 005 y sus reglas de asistencia. No inventar porcentajes para justificada/tarde ni rellenar períodos no registrados.

## Criterios de aceptación propuestos

- **INF-01:** Dado un estudiante con registros, cuando se genera su informe, entonces el PDF identifica el contexto académico e incluye únicamente sus estados y períodos dentro del alcance elegido.
- **INF-02:** Dados registros sin clase, cuando se muestra la estadística, entonces no se incluyen como ausencias ni oportunidades de asistencia.
- **INF-03:** Dada una política aprobada, cuando se genera el cálculo, entonces coincide con los casos numéricos de 005 y muestra una explicación suficiente para interpretarlo.
- **INF-04:** Dado un estudiante sin registros en el alcance elegido, cuando se genera el informe, entonces comunica que no hay registros y no presenta un porcentaje ficticio.
- **INF-05:** Dado un error de generación o escritura, cuando se exporta, entonces se informa el fallo y no se comunica un PDF válido inexistente.
- **INF-06:** Dado un informe con varios períodos/páginas, cuando se abre el PDF, entonces el contenido sigue siendo legible y no hay filas cortadas ni datos de otros estudiantes.

## Estados de UI

Sin datos: aviso de ausencia de registros. Cargando: generación. Éxito: ruta del PDF generado y apertura cuando corresponda. Error: destino inválido o generación fallida, con reintento. Cancelación: no guardar un archivo incompleto.

## Modelo y migraciones

Proyección de lectura de Estudiante, GrupoAcademico, Corte y RegistroAsistencia. No se propone una tabla de informes; cualquier persistencia adicional deberá justificarse. Plantilla, metadatos y alcance pendientes de D-08.

## Seguridad y errores

Exportación local explícita; no envío automático. Minimizar datos personales y definir los campos necesarios. No sobrescribir archivos existentes sin una elección explícita del docente.

## Pruebas previstas

Contenido/alcance por estudiante, cálculo aprobado, sin registros, períodos sin clase, rutas sin permiso, múltiples páginas y revisión visual de PDF con datos ficticios. Seleccionar el generador tras auditoría de licencia y dependencias.

## Decisiones abiertas

D-07/D-08: fórmula, alcance por corte/varios cortes, campos de identificación, encabezados, fecha y tratamiento de datos pendientes. Aprobar una muestra del PDF antes de cerrar el módulo.
