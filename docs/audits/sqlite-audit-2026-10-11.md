# Auditoría integral SQLite — AulaRegistro

**Fecha:** 2026-10-11, America/Bogota. **Rama:** main. **Base de historial:** a900c0e; diagnóstico sobre el trabajo local posterior, incluidas las correcciones de 013. **Estado:** diagnóstico; las recomendaciones no se han aplicado. La autorización posterior de commit/push permite publicar el informe, no se interpreta como autorización de sus cambios de producción.

**Skill aplicada:** sqlite-dotnet-audit 1.0.0, proporcionada por el docente en aula-registro-sqlite-skill.zip. Se leyeron íntegramente SKILL.md y sus dos referencias; el paquete solo contiene Markdown, sin scripts ni dependencias ejecutables. Se conserva en `.agents/skills/sqlite-dotnet-audit/` para futuras auditorías.

**Stack observado:** C#/.NET 10; SDK portátil 10.0.401; runtime de las sondas 10.0.12; Avalonia 11.3.13; Microsoft.Data.Sqlite/Core 10.0.12; SQLitePCLRaw 2.1.12 según packages.lock.json. `SELECT sqlite_version()` devolvió **3.53.3** en el entorno probado. No hay EF Core, DbContext, Dapper, migraciones EF ni servicio remoto. No se recomienda introducirlos para solucionar los problemas encontrados.

**Método:** lectura de código/SDD, compilación y pruebas existentes, sondas de consola independientes sobre bases temporales con estudiantes ficticios, EXPLAIN QUERY PLAN y PRAGMA de lectura. El programa de sondas, la copia de trabajo de la skill, resultados JSONL y compilaciones están en `.tools/sqlite-audit/`, excluidos de Git. No se abrió, copió ni modificó una base académica real. No se hicieron cambios de código de producción, esquema o migraciones durante la auditoría. La revisión visual nativa permanece manual por indicación del docente.

## 1. Cobertura de extremo a extremo

| Módulo | Recorrido real | Persistencia / lectura | Estado |
|---|---|---|---|
| Inicio / Mis Materias | HomeView → HomeViewModel / CatalogEditorWindow → CatalogEditorViewModel → CatalogService → SqliteCatalogStore | semesters y courses; lectura de cantidad de alumnos desde JSON del workspace | Implementado; consultas y creación sincrónicas |
| Abrir materia / corte | MainViewModel.OpenCourse / SelectedCut → CatalogService.Open → WorkspaceService → CourseStore.Load | SELECT de schema_version, revision y payload por course_id; cambiar corte usa memoria | Implementado; abrir una materia v1 puede persistir la conversión a v2 |
| Principales / subactividades | ConfigurationWindow → ConfigurationViewModel → ConfigureGroupsAsync → CourseStore.Save | Edición del borrador sin SQL; guardar publica una instantánea completa de course_workspaces en una transacción | Implementado; worker para preparación/escritura; notificación posterior en contexto del llamador |
| Notas | GradeEditor Enter / LostFocus → GradeCellViewModel.Commit → WorkspaceService.SetGrade → Change → CourseStore.Save → Changed → MainViewModel.Rebuild | Toda la instantánea, incluidos los tres cortes y retirados | Implementado; guardado y reconstrucción sincrónicos en este flujo |
| Importación Excel | MainWindow.OpenImport → lector XLSX en worker → StudentImportViewModel.Save → WorkspaceService.ImportStudents → CourseStore.Save | Solo nombre/documento; lote completo; no SQL por alumno | Implementado; confirmación y refresco siguen sincrónicos |
| Asistencias / semanas | AttendanceCellViewModel / WeeksViewModel → SetAttendance / AddWeek / RemoveWeek → Change → CourseStore.Save | Semanas y estados dentro del JSON del corte | Implementado; escritura y refresco sincrónicos |
| Recuperar retirados | RecoveryViewModel → RestoreActivity / RestoreWeek, o ConfigurationViewModel para principal → WorkspaceService | Archivo lógico de actividades/grupos/semanas dentro del mismo payload; notas originales conservadas | Implementado; sin borrado definitivo |
| Informes | MainWindow.ExportReport → clon de workspace → AcademicReportProjection / XlsxReportExporter → ReportFile | Usa memoria del workspace; no consultas independientes a SQLite | Adaptador Excel conservado sin acceso visible; PDF pendiente; proyección contiene búsquedas repetidas en listas |
| Respaldos / restaurar / Drive | BackupsView informativa; specs/007 y 009 | Ningún BackupDatabase, copia de base, restauración, OAuth o subida implementados | Pendiente reconocido; no hay exportaciones simuladas |
| Demostración | App en modo demo → SqliteWorkspaceStore → WorkspaceService | demo_workspace(id=1, schema_version, payload); separado del catálogo real | Implementado; sin control de revisión optimista entre dos demos |

Entidades académicas: AcademicWorkspace, AcademicCut, EvaluationGroup, EvaluationActivity, GradeEntry, Student, AttendanceWeek, AttendanceEntry y retirados. Los ViewModels no contienen SQL ni conexiones. El contrato IWorkspaceStore y la infraestructura aíslan el almacenamiento.

## 2. Resumen de prioridades

P0 significa pérdida/cierre inminente demostrado; P1, bloqueo o integridad importante; P2, rendimiento, seguridad o recuperación a corregir; P3, mejora acotada. **No se confirmó un P0 en el flujo normal actual.** Sí hay errores con fixtures deliberadamente inconsistentes que pueden escapar de los límites de UI, y un bloqueo reproducible con listados grandes. Los hallazgos de seguridad son sobre datos locales y recuperación; no se encontró una superficie de acceso remoto a la base.

| ID | Gravedad / estado | Hallazgo | Archivos / líneas principales |
|---|---|---|---|
| AUD-01 | P1 confirmado y medido | Reconstrucción de ambas tablas con búsquedas repetidas; coste que crece aproximadamente con el cuadrado del listado denso | MainViewModel.cs:156,185; AcademicRows.cs:25,57,100; GradeCalculator.cs:19,26 |
| AUD-02 | P1 confirmado | Notas, asistencias, semanas, importación y catálogo siguen esperando I/O en el hilo llamador; contención puede bloquear UI | WorkspaceService.cs:32,61; AcademicRows.cs:27,64; WeeksViewModel.cs:22; StudentImportViewModel.cs:71; CatalogEditorViewModel.cs:22 |
| AUD-03 | P1 confirmado en fixtures; cierre nativo no reproducido en esta auditoría | Carga acepta inconsistencias académicas/estructurales que causan excepciones posteriores fuera de los filtros actuales | WorkspaceService.cs:13,26; SqliteCatalogStore.cs:39,65; MainViewModel.cs:161,183; MainWindow.axaml.cs:21 |
| AUD-04 | P1 confirmado | Un error de notificación posterior al commit puede comunicarse como «No se guardó» en notas y otros flujos | WorkspaceService.cs:37,61; AcademicRows.cs:41,76; WeeksViewModel.cs:22; StudentImportViewModel.cs:78 |
| AUD-05 | P2 confirmado | Cada cambio unitario clona, serializa y reescribe todo el workspace; serialización dentro de la transacción productiva | WorkspaceService.cs:31,36; SqliteCatalogStore.cs:72,73 |
| AUD-06 | P2 confirmado en código | Restricciones SQL no validan la estructura/integridad del contenido JSON ni toda la coherencia catálogo/workspace | SqliteCatalogStore.cs:23,25,66; AcademicModels.cs; AcademicRules.cs |
| AUD-07 | P2 riesgo local confirmado; permisos efectivos pendientes | Datos académicos sin cifrado local y sin política explícita de permisos/ubicaciones alternativas | App.axaml.cs:26; SqliteCatalogStore.cs:14; .gitignore |
| AUD-08 | P2 confirmado; recuperación pendiente | Migración de payload v1→v2 al abrir, sin respaldo recuperable previo automatizado | WorkspaceService.cs:17,27; SqliteCatalogStore.cs:64,73; specs/012 |
| AUD-09 | P2 confirmado | Diagnóstico desigual y excepciones no cubiertas en flujos distintos de configuración | CatalogEditorViewModel.cs:30; AcademicRows.cs:41,76; WeeksViewModel.cs:22,45; App.axaml.cs:30; Program.cs:13 |
| AUD-10 | P3 confirmado por plan; beneficio de nuevo índice no medido | ORDER BY de materias necesita B-tree temporal; lectura de conteos JSON y refrescos de catálogo en cada cambio de nota | SqliteCatalogStore.cs:39; MainViewModel.cs:156; HomeViewModel.cs:39 |
| AUD-11 | P3 confirmado, solo demo | DemoStore no contrasta versión SQL con versión del JSON y no evita sobrescrituras entre instancias | SqliteWorkspaceStore.cs:26,28,36 |
| AUD-12 | P2 riesgo operativo / funcionalidad pendiente | Todavía no existe recuperación por backup del archivo ante pérdida del equipo o daño de base | BackupsView.axaml:5; specs/007-respaldos/spec.md |

Las líneas corresponden al código local auditado. Rutas relativas: `src/GestorDocente.Desktop/ViewModels/`, `src/GestorDocente.Desktop/Views/`, `src/GestorDocente.Application/`, `src/GestorDocente.Infrastructure/` y `src/GestorDocente.Domain/` según cada clase.

## 3. Evidencia y recomendaciones por hallazgo

### AUD-01 — Cálculos y refrescos completos

**Causa:** cada Changed crea GradeRowViewModel y AttendanceRowViewModel de todos los estudiantes visibles, recalcula el promedio de todo el curso y reconstruye columnas. Cada celda/resultado utiliza SingleOrDefault sobre la lista completa de notas o asistencias. El número de registros de esas listas crece con el número de alumnos. No es un N+1 de SQL: el trabajo dominante ocurre en memoria y en el hilo de UI. La virtualización visual del DataGrid no evita construir todos estos ViewModels.

**Evidencia:** con 1.000 alumnos, 15.000 notas y 9.000 asistencias, crear MainViewModel tarda 10.720,5 ms; cambiar una nota con sus suscriptores tarda 10.457,4 ms. La escritura del último ensayo tarda 8,9 ms. El servicio sin ViewModels tarda 28,9 ms. Son sondas de consola de la lógica MVVM, sin coste del renderizado nativo: el cuello de botella ya existe antes de dibujar.

**Recomendación mínima:** índices de consulta en memoria por (corte, estudiante, actividad/semana), calculados una vez por snapshot; validar configuración una vez; actualizar fila y agregados afectados, y no reconstruir asistencia/columnas al cambiar solo una nota. Mantener decimales y promedio simple. **Alternativa:** normalización y consultas por estudiante, solo después de una ADR y mediciones; no es necesaria para empezar. **Riesgo/migración:** sin cambio de esquema para la alternativa mínima; cuidar invalidación al cambiar cortes, retirar/restaurar y editar configuración. **Pruebas:** resultados exactos, cero y pendientes; listas densas 100/500/1.000; notificaciones de una fila, selección y teclado preservados; incremento acotado del tiempo y eventos de UI atendidos.

### AUD-02 — I/O sincrónico y contención

**Causa:** la corrección 013 trasladó únicamente ConfigureGroupsAsync a Task.Run. El resto de comandos continúa usando Change / ImportStudents / catálogo sincrónicos. DefaultTimeout productivo es 5 segundos. El proveedor reintenta SQLITE_BUSY/LOCKED hasta el timeout; el PRAGMA busy_timeout observado en la sonda es 0 y no representa por sí solo esos reintentos del proveedor.

**Evidencia:** una transacción escritora retenida en la base ficticia hizo que SetGrade esperara **5.155,6 ms**, terminara con SqliteErrorCode=5 y ExtendedErrorCode=5 y conservara el valor previo. El código de los eventos llama ese mismo método sin liberar el hilo de UI. No se activó WAL ni se cambiaron timeouts.

**Recomendación mínima:** unidades de trabajo fuera de UI para los demás cambios, conexiones por operación, una política acotada de escritura por workspace y publicación en Dispatcher/contexto de UI. Conservar entradas mientras esperan o fallan y prevenir envíos duplicados. **Alternativa:** evaluar WAL después de medir concurrencia real en almacenamiento local y definir durabilidad; no resuelve múltiples escritores ni los cálculos de AUD-01. **Riesgo/migración:** async no requiere esquema nuevo; orden de eventos y concurrencia deben probarse. **Pruebas:** escritor bloqueado, timeout, liberación/reintento, dos ventanas y dos procesos; la UI sigue procesando eventos, ningún valor no confirmado se pierde. Los métodos ADO.NET con sufijo Async de este proveedor no garantizan I/O asincrónico: [documentación de Microsoft](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async), [errores y timeouts](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors).

### AUD-03 — Hidratación incompleta

**Causa:** al cargar se comprueban versión, CourseId, IsDemo, tres cortes y porcentajes/IDs de grupos. No se validan todas las colecciones, unicidad de (alumno, nota/semana), estados permitidos, número de semanas, duplicados de alumnos/documentos ni todas las referencias activas/retiradas. JSON válido sintácticamente no significa workspace válido. Courses usa json_array_length y GetInt32 sin tratar campo ausente/null.

**Evidencia sobre bases ficticias modificadas deliberadamente:**

| Alteración | Carga WorkspaceService | Consumidor posterior |
|---|---|---|
| Nota duplicada con misma clave | Aceptada | InvalidOperationException en búsquedas SingleOrDefault |
| Número de semana duplicado | Aceptada | ArgumentException al crear diccionario de celdas |
| Diez semanas activas | Aceptada | Sin error; infringe máximo de nueve |
| Estado de asistencia enum 99 | Aceptada | Sin rechazo; no representa P/A/J/T |
| Students=null | Aceptada | NullReferenceException al construir ViewModels |
| Campo Students ausente | Aceptada | InvalidOperationException en el catálogo/contador |
| SemesterId del JSON diferente del catálogo | Aceptada | Sin rechazo; contexto incoherente |

MainViewModel.Try no captura ArgumentException, InvalidOperationException o NullReferenceException. Hay eventos async void de apertura sin frontera de diagnóstico general. **No se afirma haber observado el cierre nativo con esos fixtures**, pero las excepciones y las brechas de filtros son comprobables. Tampoco se afirma que la UI sana produzca tales archivos: simulan daño lógico, versiones externas o una restauración futura defectuosa.

**Recomendación mínima:** validador completo al cargar y antes de convertir/guardar; error de compatibilidad/integridad explícito y conservar archivo original. Distinguir referencias archivadas válidas de huérfanos. Apertura protegida que no deje Service sustituido a medias. **Alternativa:** formato DTO de persistencia separado con validación de campos requeridos. **Riesgo/migración:** empieza sin esquema nuevo; no «reparar» eliminando notas ni convertir faltantes en ceros. **Pruebas:** matriz anterior, grades fuera de 0–5, alumnos/documentos duplicados, IDs vacíos, referencias activas/retiradas, campo faltante, JSON corrupto/truncado, versiones futuras y apertura sin cerrar proceso.

### AUD-04 — Resultado de escritura frente a resultado de publicación

**Causa:** store.Save y Current se confirman antes de invocar Changed. Si un suscriptor falla, la misma excepción llega al bloque que informa un guardado fallido. ConfigurationViewModel ya distingue estos casos desde 013, pero GradeCellViewModel, asistencia, importación y semanas aún no.

**Evidencia:** suscriptor ficticio que lanza IOException después del commit; GradeCellViewModel.Error dice **«No se guardó la nota. Reintenta.»**, pero reabrir SQLite devuelve **4,8**, la nota nueva.

**Recomendación mínima:** resultado de operación que distinga Persisted de RefreshFailed; error de notificación diagnosticado sin negar un commit correcto ni repetirlo innecesariamente. **Alternativa:** desacoplar publicación y reconstrucción mediante un coordinador de presentación. **Riesgo/migración:** sin migración; conservar visibilidad de excepciones, no ignorar suscriptores. **Pruebas:** excepción antes de escritura, en commit y después de commit; mensajes y revisión persistida correctos, sin duplicaciones al reintentar.

### AUD-05 — Instantánea completa y duración de transacción

**Causa:** Clone usa serialize→deserialize de todos los cortes y retirados; CourseStore.Save vuelve a serializar después de BeginTransaction. Una nota reescribe el mismo JSON que toda la configuración. Datos retirados se conservan deliberadamente, así que también aumentan el coste con el uso prolongado.

**Evidencia:** payloads densos medidos: 124.697 B (100 alumnos), 625.097 B (500), 1.250.597 B (1.000). El cambio de una sola nota los reescribe; el coste del servicio sube aunque la sentencia UPDATE es una. Actualmente esto es secundario al refresco cuadrático, no justifica cambiar toda la arquitectura de inmediato.

**Recomendación mínima:** preparar/validar/serializar antes de tomar el bloqueo escritor y evitar guardados sin cambio; medir asignaciones, memoria y tamaño con retirados. **Alternativa posterior:** persistencia granular manteniendo la atomicidad del agregado, mediante ADR/migración aprobadas. **Riesgo:** debe conservarse revision optimista y publicación después del commit; trasladar operaciones no debe compartir conexiones activas entre hilos. **Pruebas:** rollback, conflicto de versión, conservación de notas/asistencias/retirados, tamaño creciente y tiempo dentro de transacción. [Transacciones SQLite](https://www.sqlite.org/lang_transaction.html).

### AUD-06 — Restricciones y coherencia del modelo persistido

**Causa:** catálogo relacional con PK/FK/UNIQUE y CHECK(term), pero payload es TEXT NOT NULL sin CHECK(json_valid(...)) ni validación de forma. schema_version/revision no tienen CHECK de rangos. IDs TEXT PRIMARY KEY no declaran NOT NULL explícitamente ni usan STRICT/WITHOUT ROWID. La FK entre courses y semesters existe; estudiantes/notas/semanas están dentro de JSON y sus relaciones dependen del dominio. CourseStore.Load no contrasta SemesterId ni metadatos del payload con courses.

**Evidencia:** esquema DDL revisado y fixtures AUD-03. foreign_key_check e integrity_check pasan en un fixture sano; eso **no certifica la integridad académica del JSON**. No se probó una explotación remota ni una entrada de UI capaz de inyectar SQL; los valores de SQL están parametrizados.

**Recomendación mínima:** límites de dominio y validación coherente en carga/escritura; revisar CHECK(json_valid), versiones/revisión e IDs NOT NULL con datos heredados. **Alternativa:** tablas normalizadas de registros académicos, solo si se aprueba. **Riesgo/migración:** las restricciones SQL nuevas requieren migración, ensayo y backup previo; no añadirlas sin revisar incompatibilidades. **Pruebas:** restricciones, rollback ante fila inválida, coherencia de metadatos, duplicados y claves nulas en bases efímeras. [Claves foráneas](https://www.sqlite.org/foreignkeys.html), [PRAGMA de integridad y FK](https://www.sqlite.org/pragma.html).

### AUD-07 — Privacidad y ubicación

**Hecho:** producción usa LocalApplicationData/AulaRegistro/aula-registro.db, separado de la demo. Nombre/documento/notas se almacenan como JSON legible en SQLite. No hay cifrado local ni verificación explícita de permisos; el acceso efectivo depende del sistema operativo. GESTOR_DOCENTE_DATA_DIR permite elegir otra ruta sin validar si es red/carpeta sincronizada. No se inspeccionaron ACL, cifrado de disco ni ubicación real de los datos del docente.

**Recomendación:** especificar protección del archivo y directorio por usuario, retención y privacidad de diagnósticos; advertir sobre ubicaciones no compatibles y documentar protección de disco. Evaluar cifrado local por separado si se requiere; cifrar backups antes de Drive según la constitución. No añadir inicio de sesión ni exigir una cuenta. **Alternativa:** proveedor cifrado aprobado con gestión de claves; no instalado ni aprobado. **Riesgo/migración:** ACL/documentación sin esquema; cifrado cambia despliegue y recuperación. **Pruebas:** cuentas del SO distintas, ruta sin permisos, rutas con caracteres y archivo académico excluido de Git. Local-first no implica que una carpeta accesible a terceros sea privada.

### AUD-08 — Conversiones y recuperación

**Hecho:** catálogo usa user_version=1; contenido de workspace tiene versiones 1/2, actuales escrituras v2. La conversión v1→v2 está autorizada por spec/012 y es atómica con revisión. Se rechazan versiones futuras. No existe respaldo preconversión independiente: si se confirma, la recuperación del formato original no está implementada.

**Recomendación:** antes de ampliaciones de migración, definir procedimiento de copia consistente, prueba histórica, verificación y recuperación. Validar el workspace completo antes de convertir, no solo grupos. **Alternativa:** conversión de una copia verificada antes del reemplazo, con control de revisión. **Riesgo/migración:** no cambiar versiones ahora; no retroceder formatos con datos reales. **Pruebas:** interrupción antes/después de commit, dos lectores antiguos, payload antiguo inválido, respaldo y recuperación exactos. [BackupDatabase de Microsoft](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup), [API de respaldo SQLite](https://www.sqlite.org/backup.html).

### AUD-09 — Observabilidad y límites de excepción

**Hecho:** configuración muestra ErrorDetails y estado de guardado; otros ViewModels convierten IOException/DbException en mensajes genéricos sin códigos SQLite ni diagnóstico duradero. Sus filtros no cubren UnauthorizedAccessException ni errores estructurales inesperados. Program solo configura LogToTrace de Avalonia, que no equivale a un registro de operaciones de persistencia. No se detectó un catch vacío.

**Recomendación mínima:** frontera de operación consistente con tipo de fallo, código SQLite/extendido, identificador de operación y mensaje accionable; conservar borrador y valores. Registro local acotado y sin alumnos/documentos, payload, credenciales ni parámetros SQL. Evitar atribuir todas las restricciones al mismo «duplicado». **Alternativa:** pequeño servicio de diagnósticos desacoplado. **Riesgo/migración:** sin esquema; no publicar logs ni detalles privados. **Pruebas:** permisos, disk-full/I/O simulado, corrupto, busy/locked, FK/UNIQUE, suscriptor fallido y excepción inesperada; diagnóstico completo sin datos personales. [Errores Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors).

### AUD-10 — Consultas e índices

**Evidencia EXPLAIN en fixture:** semestre ordenado usa sqlite_autoindex_semesters_2; búsqueda de materias usa sqlite_autoindex_courses_2 por semester_id y sqlite_autoindex_course_workspaces_1 en el join; requiere **USE TEMP B-TREE FOR ORDER BY**. Carga por course_id usa su índice primario. No hay escaneo completo injustificado confirmado para cargar una materia.

**Recomendación:** medir catálogo de varios años y muchas materias antes de crear índice (semester_id,name,group_name). Separar el contador del JSON o cachearlo si el coste lo justifica. No refrescar todo Home al cambiar una nota, pues la cantidad de alumnos no cambia. **Alternativa:** contador relacional actualizado atómicamente; implica esquema/migración. **Riesgo:** todo índice aumenta trabajo de escritura; mantener ordenación y conteo correctos. **Pruebas:** planes y tiempo con catálogo grande, importación actualiza conteo, cambios de notas no consultan catálogo. [EXPLAIN QUERY PLAN](https://www.sqlite.org/eqp.html).

### AUD-11 — Demo y versiones

**Evidencia:** fila demo schema_version=2 con payload.SchemaVersion=999: SqliteWorkspaceStore.Load devuelve el JSON sin rechazarlo. WorkspaceService sí rechaza 999 después: existe una segunda defensa; **no es una sobrescritura de producción demostrada**. La demo tampoco usa revisión optimista.

**Recomendación:** igualar los contratos de versión/identidad de demo y producción y documentar o bloquear dos instancias de demo. **Riesgo/migración:** restringido a datos de demostración; respetar bases demo previas. **Pruebas:** discrepancias 1/2/999, IsDemo falso y dos escritores. No dar prioridad sobre AUD-01–04.

### AUD-12 — Backups todavía pendientes

**Hecho:** la pantalla informa honestamente «Módulo en preparación» y los servicios de backup/restauración no existen. No se encontró copia en caliente peligrosa ni éxito ficticio. Es una carencia de recuperación para uso de años, no una función implementada defectuosamente.

**Recomendación:** siguiente etapa SDD de respaldo local consistente y restauración verificable, antes de Drive y de nuevas migraciones significativas. Conservar originales y probar compatibilidad; nunca copiar un archivo abierto ignorando journaling. **Riesgo:** formato, cifrado y claves requieren aprobación propia. **Pruebas:** backup mientras hay escrituras, integridad/FK/dominio, truncado/corrupto, clave errónea, versión futura, interrupción de restauración y reversión. [Respaldos consistentes](https://www.sqlite.org/backup.html).

## 4. Mediciones actuales y configuración

Se midió código actual Debug en Windows, runtime 10.0.12, 8 procesadores lógicos; 5 grupos × 3 subactividades, notas completas, 9 semanas P, una materia por base temporal. Mediana de 3 repeticiones tras una ejecución de calentamiento. Añadir 10 filas es solo lógica del ViewModel, sin renderizado. La columna persistencia es el último ensayo instrumentado del store, no una mediana independiente. No comparar estos valores con producción en otro equipo ni extrapolar tiempos a 5.000 alumnos.

| Alumnos | Notas / asistencias | Payload B | Agregar 10 (ms) | Crear MainVM (ms) | SetGrade con suscriptores (ms) | Persistencia último ensayo (ms) | SetGrade sin UI (ms) |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 100 | 1.500 / 900 | 124.697 | 0,0083 | 146,1 | 149,4 | 4,6 | 11,0 |
| 500 | 7.500 / 4.500 | 625.097 | 0,0047 | 2.511,6 | 2.519,1 | 7,5 | 13,1 |
| 1.000 | 15.000 / 9.000 | 1.250.597 | 0,0043 | 10.720,5 | 10.457,4 | 8,9 | 28,9 |

**Antes/después:** no hay cambios de implementación en esta auditoría, por tanto no se publica una comparativa antes/después ficticia. 013 ya corrigió la colección observable, desfase de columnas y worker de configuración; sus diez pruebas siguen vigentes. El nuevo diagnóstico identifica el coste que permanece al publicar tablas densas.

**Conteo SQL:** no hubo interceptor/traza SQL global; lo siguiente es un conteo derivado del código: agregar subactividades = 0 sentencias de persistencia; guardar workspace = 1 UPDATE más gestión de transacción; después MainVM consulta Home (semestres y cursos). No es una medición de todas las instrucciones internas del proveedor. No hay una consulta por alumno.

| Lectura PRAGMA / motor en fixture productivo | Valor |
|---|---|
| sqlite_version() | 3.53.3 |
| foreign_keys | 1 (ForeignKeys=true) |
| journal_mode | delete |
| synchronous | 2, FULL |
| busy_timeout | 0; el timeout/reintento del proveedor se observa por separado |
| DefaultTimeout de la cadena | 5 segundos |
| user_version | 1 |
| integrity_check | ok |
| foreign_key_check | 0 filas |

Son valores de una base **nueva ficticia**, no lecturas de la base real. No se ejecutaron asignaciones PRAGMA journal_mode/synchronous, VACUUM ni reparaciones. No se puede concluir el modo de una base preexistente únicamente a partir del código.

WAL no está activo en el fixture y no se prescribe automáticamente. Si se evalúa después, debe ser almacenamiento local, con política de durabilidad y checkpoints. La versión nativa observada supera 3.51.3, versión donde la documentación sitúa la corrección del WAL-reset bug; no se marca este entorno como afectado. Es necesario verificar el motor empacado en cada distribución antes de activar WAL. [Documentación oficial WAL y WAL-reset](https://www.sqlite.org/wal.html).

## 5. Integridad, seguridad y decisiones académicas

- **Correcto hoy:** parámetros SQL para todos los valores; comandos/conexiones/readers/transactions de vida corta; FK explícitas productivas; creación de materia+workspace en la misma transacción; workspace atómico y revisión optimista; versiones futuras rechazadas; producción fuera del repo; demo separada; no tokens ni dependencias SaaS en almacenamiento.
- **Git:** bases, diarios WAL/SHM/journal, claves y archivos de credenciales quedan excluidos. Revisión de candidatos sin firmas habituales de secretos y con evidencias visuales de demostración. Esto no certifica todas las variantes posibles de secretos ni implica cifrado del archivo local.
- **Académico:** grupos principales libres con 100 % por corte, nota directa opcional y promedio aritmético simple de subactividades, decimal 0–5, exactamente tres cortes. No existe restricción por nombre Parcial ni rúbrica en código activo. Faltantes continúan pendientes, cero válido. P/A/J/T, nueve semanas; semana de actividades admitida. Porcentajes J/T y pesos finales de los cortes siguen sin definición: no se inventan durante esta auditoría.
- **Persistencia de decimal:** es un número JSON de System.Text.Json, leído/calculado con decimal, no una columna REAL ni una consulta SQL de promedios. No se propone migrarlo a double o enteros escalados sin una decisión aprobada.
- **EF Core:** no aplica AsNoTracking, SaveChanges, DbContextFactory, EnsureCreated, __EFMigrationsLock ni reconstrucción de tabla de migraciones EF al código actual. Si se adopta en el futuro, revisar [vida de DbContext](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/) y [limitaciones del proveedor](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations); no usar recomendaciones EF como justificación de cambios ahora.

## 6. Pruebas, límites y reproducción

Compilación completa: **0 errores, 0 advertencias**. Suite: **85 aprobadas, 0 fallidas, 0 omitidas**: 14 dominio, 26 aplicación, 17 infraestructura, 28 aceptación. Las sondas adicionales son diagnósticos aislados, **no** nuevas pruebas permanentes ni funcionalidades de producción.

Las 85 pruebas no cubren el rendimiento denso de AUD-01, toda la corrupción lógica de AUD-03 ni backups todavía inexistentes. Que pasen no significa ausencia de problemas. No se ejecutó una comparación visual nativa, pruebas de macOS, cortes de energía, disco lleno real, filesystem de red, permisos de otros usuarios ni carga de 5.000 alumnos. No se modificaron bases reales para «verificar» esos escenarios.

Reproducción diagnóstica: crear catálogo temporal mediante SqliteCatalogStore; crear un semestre y materia; semillar alumnos ficticios, cinco grupos con tres subactividades, notas 4 y nueve semanas presentes; guardar una vez mediante el store. Construir MainViewModel y cronometrar SetGrade con/sin suscriptores. Retener una transacción en una segunda conexión para probar busy. Para integridad, usar una base nueva por cada alteración de la tabla AUD-03. Recargar por CatalogService.Open y construir MainVM dentro del límite de la sonda. Nunca aplicar esas alteraciones al archivo del docente.

Comandos de verificación usados (artefactos ignorados):

```powershell
.\.tools\dotnet\dotnet.exe build GestorDocente.slnx --artifacts-path .tools/build-013 --no-restore
.\.tools\dotnet\dotnet.exe test GestorDocente.slnx --artifacts-path .tools/build-013 --no-build --no-restore
```

Las sondas temporales quedan disponibles localmente en `.tools/sqlite-audit/Program.cs`, `Probe.csproj` y `results.jsonl`; no se publican archivos de base ni herramientas del equipo. La evidencia relevante y parámetros se transcriben aquí.

## 7. Propuesta por etapas SDD + TDD — pendiente de autorización de implementación

| Etapa | Hallazgos | Aceptación antes de código | Reversión / migración |
|---|---|---|---|
| A: integridad y errores | AUD-03,04,09 | Carga inconsistente rechazada sin alterar original; permisos y fallos inesperados no cierran aplicación; estado del commit comunicado correctamente | Sin migración inicial; commit aislado; conservar bases históricas |
| B: rendimiento en memoria | AUD-01,10 | Misma nota/corte/estado; búsquedas indexadas y actualización por fila; comparación antes/después con mismos fixtures; selección/teclado estables | Sin esquema; revertir código; no alterar reglas ni los datos |
| C: ejecución de escrituras | AUD-02,05 | UI responde bajo bloqueo de 5 s; transacción breve; una operación por unidad lógica, sin pérdida, con reintento seguro y publicación en UI | Sin esquema mínimo; conservar revisión; revertir coordinador si falla |
| D: recuperación local | AUD-07,08,12 | ADR de formato/cifrado/claves; BackupDatabase consistente, verificación y restauración reversible; prueba de conversión en copia | Copia previa obligatoria y procedimiento de recuperación aprobado |
| E: límites de almacenamiento | AUD-06,10,11 | Validar necesidad de CHECK/NOT NULL/índices o modelo granular, datos heredados y planes SQL; demo protegida | Migración solo si aprobada; fixtures históricos, backup y rollback documentados |

La autorización de subir este informe no autoriza activar WAL, cambiar paquetes, introducir EF Core, normalizar tablas ni modificar datos o migraciones reales. Cada etapa necesita especificación y alcance revisados antes de implementar.

## 8. Cambios de esta auditoría

Solo este informe y los Markdown de la skill/índice de skills para conservar la activación solicitada. Las fuentes, pruebas, SDD, configuraciones y locks existentes conservaron sus hashes durante el diagnóstico. Compilaciones, sonda y bases ficticias se generaron fuera de código de producción. El commit que incluye el informe puede contener los incrementos 009–013 pendientes de sesiones anteriores; no son cambios hechos por esta auditoría.
