---
name: sqlite-dotnet-audit
description: Audita, diseña y mejora SQLite en aplicaciones de escritorio C#/.NET con Avalonia y EF Core o Microsoft.Data.Sqlite. Usar al revisar persistencia, DbContext, modelos, migraciones, consultas, índices, guardar actividades y subactividades, notas, asistencias, errores al guardar, rendimiento, integridad, copias y restauración de bases .db. Sigue SDD y TDD, diagnostica antes de cambiar y protege datos académicos.
compatibility: Codex y agentes compatibles con Agent Skills; C#/.NET 10; Avalonia; SQLite con EF Core y/o Microsoft.Data.Sqlite. No requiere instalación de scripts ni dependencias externas.
metadata:
  author: "AulaRegistro (skill personalizada)"
  version: "1.0.0"
---

# SQLite / .NET: auditoría y buenas prácticas para AulaRegistro

## Cuándo utilizar esta skill

Actívala cuando se investiguen lecturas/escrituras de SQLite, errores al guardar, lentitud de la interfaz causada por persistencia, concurrencia, definición de entidades, índices, migraciones, respaldos o restauraciones. Evalúa tanto código de infraestructura como sus consumidores en dominio, aplicación, ViewModels y tests.

## Principios no negociables

1. **SSOT:** leer primero `AGENTS.md`, `.specify/memory/constitution.md` y las especificaciones SDD actuales. Respetar decisiones recientes: la rúbrica fue retirada del alcance de evaluaciones; no reintroducirla por encontrar documentos antiguos. Confirmar divergencias antes de cambiar reglas.
2. **Seguridad de datos:** nunca ejecutar migraciones, limpieza, `VACUUM`, `PRAGMA journal_mode`, `DROP`, eliminación de archivos o restauraciones sobre una base real sin autorización expresa, backup verificable y plan de reversión. Para auditoría usa base efímera o una **copia consistente**.
3. **Auditoría primero:** no refactorizar a ciegas; elaborar inventario, reproducir fallos, medir, priorizar hallazgos y preparar plan. Corregir únicamente tras acordar alcance; cambios mínimos y pruebas.
4. **Cero pérdida silenciosa:** si falla un guardado, mostrar diagnóstico útil, conservar valores no persistidos y registrar la excepción sin datos personales. No capturar excepciones para ignorarlas; no terminar el proceso silenciosamente.
5. **Tecnología real:** determinar si el proyecto usa EF Core, `Microsoft.Data.Sqlite`, Dapper o mezclas antes de aplicar recomendaciones. No incorporar ORMs o paquetes nuevos sin justificación y autorización.
6. **Sin datos personales en evidencias:** usar estudiantes ficticios en capturas, logs, fixtures y reportes; no incluir archivos `.db` ni tokens en Git.
7. **Diferenciar hechos e hipótesis:** cada hallazgo incluye archivo y línea, condición reproducible, efecto, evidencia y prueba. Si falta acceso al código o a la ejecución, indicarlo.

## Secuencia obligatoria de trabajo

### Fase A — Inventariar sin modificar

1. Leer reglas SDD y ejecutar `git status` antes de actuar; respetar cambios preexistentes.
2. Identificar soluciones `.sln`/`.slnx`, proyectos `src/`, `tests/`, versiones reales de paquetes en `.csproj` y `Directory.Packages.props`.
3. Buscar referencias a `DbContext`, `IDbContextFactory`, `DbSet`, `SqliteConnection`, `SqliteCommand`, `SaveChanges`, SQL crudo, `Database.Migrate`, `EnsureCreated`, cadenas de conexión, repositorios, interceptores, background tasks y servicios de respaldo.
4. Trazar para cada módulo el camino: evento de UI -> ViewModel -> aplicación -> repositorio/servicio -> conexión/DbContext -> tabla(s). Incluir materias, grupos, cortes, actividades/subactividades, notas, importación de estudiantes, asistencias, informes y copias/restauración.
5. Revisar esquema real y migraciones: claves primarias/foráneas, restricciones `CHECK`, `UNIQUE`, columnas nulas, índices, política de borrado y versionado de esquema; no asumir que existen.
6. Registrar persistencia pendiente o simulada (datos en memoria) como tal. No inventar fallos ni afirmar que una base exista cuando solo haya contratos.

**Búsquedas sugeridas** (lectura; adaptar a estructura y herramientas disponibles):

```powershell
rg -n -g "*.cs" -g "*.axaml" "DbContext|IDbContextFactory|SqliteConnection|SqliteCommand|SaveChanges|BeginTransaction|Migrate\(|EnsureCreated\(|ExecuteSql|FromSql|Task\.Run|Dispatcher" src tests
rg -n -g "*.cs" "AddActivity|Subactivity|SubActividad|Actividad|Taller|Quiz|Attendance|Asistencia|BackupDatabase" src tests
```

Si `rg` no existe, usar búsqueda nativa de VS Code o PowerShell. No imprimir archivos de secretos ni datos reales.

### Fase B — Diagnosticar operaciones críticas

**1. Creación de actividades y subactividades:**
- Determinar si se guarda en cada pulsación/cambio de campo, en cada creación de fila o únicamente al confirmar; registrar frecuencia de `SaveChanges`.
- Buscar consultas repetidas por elemento (N+1), validaciones con lecturas de toda la tabla, reconstrucción completa del ViewModel/DataGrid, notificaciones circulares, eventos duplicados y bloqueo del hilo UI.
- Revisar posible uso compartido simultáneo del mismo `DbContext`: EF Core **no es thread-safe**. Usar contextos de vida corta por unidad de trabajo; no conservar uno global para toda la ventana.
- Para cambios compuestos aplicar unidad de trabajo/transaction cuando haga falta atomicidad: actividad principal y subactividades deben quedar juntas o no quedar. No abrir transacciones largas durante edición visual.
- Medir tiempo de UI, validación, materialización, SQL y commit **antes y después** con datos ficticios. Evitar `SaveChanges` redundante por cada subactividad si un único guardado lógico es suficiente.

**2. Cierre inesperado tras Guardar:**
- Reproducir desde la UI y recopilar stack trace y error real de forma privada (sin datos personales).
- Revisar `async void` fuera de eventos, `async` sin `await`, excepciones no observadas, cierre inadvertido de ventanas, dependencias liberadas, restricciones SQL, errores de migración, concurrencia, `SQLITE_BUSY/LOCKED`, clave duplicada, claves foráneas y rutas del archivo.
- Nunca resolver el problema con un `catch {}`. Añadir captura segura en el límite de UI, mensajes de error útiles, rollback y posibilidad de reintentar. Separar la causa de persistencia de la de cierre de proceso.
- Verificar dos clics seguidos en Guardar, Guardar sin subactividades y Guardar con varias; si la operación ya está activa, prevenir guardados concurrentes de la misma unidad.

**3. Consultas e índices:**
- Inspeccionar SQL generado por EF y medir número/duración de consultas sin registrar valores sensibles.
- Proyectar únicamente columnas necesarias; `AsNoTracking()` en lecturas realmente no editables; evitar cargar colecciones completas de estudiantes al realizar cambios unitarios.
- Utilizar `EXPLAIN QUERY PLAN` en **copia/fixture**, y proponer índices alineados con filtros/join/orden reales; verificar con mediciones, no agregar índices genéricos indiscriminadamente.
- Toda SQL dinámica debe parametrizar **valores**. Si hay identificadores SQL dinámicos, usar únicamente listas permitidas y validadas. Evitar concatenar entrada del docente en SQL.

**4. Concurrencia, WAL y durabilidad:**
- SQLite admite varios lectores, pero **un escritor simultáneo**. Identificar escritores paralelos, cierres de conexiones y bloqueos.
- Revisar `busy_timeout`/`Default Timeout` como salvaguarda acotada, no como sustituto de corregir contención. No aplicar reintentos infinitos.
- Valorar WAL para una base **local** y comprobar si ya está activo (EF Core puede habilitarlo al crear la DB). No activarlo sin validar ubicación y despliegue: WAL no está diseñado para una base compartida por equipos a través de red.
- No utilizar `PRAGMA synchronous=OFF` ni `journal_mode=OFF`. En WAL, `synchronous=NORMAL` puede perder transacciones recientes tras corte de energía; valorar `FULL` cuando la durabilidad de notas sea prioritaria y documentar el equilibrio con rendimiento.
- En `Microsoft.Data.Sqlite`, los métodos ADO.NET `*Async` pueden realizar I/O de forma síncrona; **no prometer** que renombrar métodos con `Async` evita bloqueo de UI. Diseñar la ejecución y el ciclo de vida de conexiones/DbContext para no bloquear la interfaz ni compartir contextos entre hilos.

### Fase C — Integridad y reglas académicas

- Verificar `PRAGMA foreign_keys=ON` en conexiones de escritura y su configuración real. **No** habilitar/deshabilitarlo dentro de transacciones; validar `PRAGMA foreign_key_check` en copia.
- Ejecutar `PRAGMA integrity_check` únicamente en copia/entorno de prueba cuando sea viable (no sustituye a `foreign_key_check`). Verificar resultado `ok`.
- Identificar restricciones y tests que impidan estudiantes duplicados dentro del grupo, porcentajes inválidos, notas fuera del rango admitido, semanas > 9 por corte y referencias huérfanas. Distinguir validaciones de dominio y de base de datos.
- Actividades: el docente define actividad principal con peso del corte; subactividades **opcionales**; promedio aritmético de subactividades cuando existan. Las actividades sin subactividades usan calificación directa. No exigir subparciales ni reintroducir rúbricas.
- Preservar nota, porcentajes y redondeo coherentes: proponer `decimal` en lógica de negocio y revisar el mapeo SQLite (no soporta `decimal` nativo con todas sus operaciones). Evaluar entero escalado para persistencia **solo si se aprueba** y hay plan de migración sin pérdida de precisión.
- Para asistencias, respetar `P/A/J/T`, no contar pendientes como ausencias y **no inventar** reglas de porcentaje para tardanzas/justificadas.

### Fase D — Migraciones, ubicación y recuperación

- Guardar datos de usuario en ruta estable del SO mediante `Environment.SpecialFolder.LocalApplicationData` o mecanismo de plataforma equivalente, no en carpeta del ejecutable/repo. Verificar permisos, uso multiplataforma y soporte para varias instalaciones.
- Revisar cómo se actualiza el esquema: preferir migraciones versionadas y probadas. Evitar mezclar `EnsureCreated` con migraciones EF Core sin estrategia deliberada. SQLite tiene operaciones de migración que requieren reconstrucciones de tabla; antes de una migración real: backup, prueba de actualización con datos históricos, rollback/restauración comprobable.
- EF Core para SQLite puede usar `__EFMigrationsLock`; investigar bloqueos huérfanos primero y **nunca borrar esa tabla automáticamente en producción**.
- Para respaldos con la aplicación abierta usar la API oficial de backup (`SqliteConnection.BackupDatabase` o mecanismo SQLite equivalente). **No** copiar a ciegas el `.db` abierto, especialmente si WAL está activo y hay archivos `-wal`/`-shm`.
- Validar respaldo consistente, integridad y compatibilidad de esquema antes de restaurar; generar copia preventiva del destino y restablecer atómicamente. No restaurar con conexiones abiertas.
- En respaldo a Google Drive, cifrar antes de subir con criptografía autenticada y gestión de claves aprobada. Nunca guardar tokens/contraseñas en la base académica o logs; el respaldo manual no equivale a sincronización multidispositivo.

### Fase E — Reporte, aprobación y corrección

**Primera entrega:** informe sin cambiar código. Ordenar incidencias P0 (pérdida/cierre), P1 (integridad/bloqueo), P2 (rendimiento), P3 (mejoras) con:

- ID, severidad, archivo y línea, ruta de ejecución, evidencia reproducible, impacto, referencia documental.
- Recomendación mínima y alternativa, riesgos, migración necesaria o no, prueba propuesta.
- Estado: confirmado / probable / pendiente de reproducción.

Guardar el informe propuesto bajo `docs/audits/sqlite-audit-AAAA-MM-DD.md` y utilizar `references/reporte-template.md` como guía. Antes de modificaciones relevantes, solicitar aprobación y presentar plan SDD por incrementos.

**Tras la aprobación:** para cada corrección: test que reproduzca -> cambio mínimo -> test y build -> medición -> revisión de migración si procede -> resumen y rollback. No hacer push automático.

## Puertas de calidad (no dar por finalizado sin revisar)

- [ ] Inventario completo de puntos de acceso a SQLite y dependencias UI -> datos.
- [ ] Errores al guardar reproducidos, diagnosticados y sin cierre de proceso.
- [ ] Guardado de actividad + subactividades atómico; sin datos parciales.
- [ ] No se guarda en cada pulsación innecesariamente ni se bloquea la interfaz.
- [ ] Contratos de dominio y restricciones comprobados con datos ficticios.
- [ ] Claves foráneas, restricciones e índices revisados; `foreign_key_check` / `integrity_check` en copias cuando aplique.
- [ ] Pruebas de lectura/escritura, concurrencia, fallos y reinicio de aplicación.
- [ ] Migraciones y recuperación planificadas sin pérdida de datos.
- [ ] No se comprometieron secretos, información personal ni bases reales.
- [ ] `dotnet build`, pruebas automatizadas y evidencia de rendimiento reportados (o bloqueo explicado).

## Bibliografía técnica

Leer `references/documentacion-oficial.md` para enlaces y notas oficiales de SQLite, Microsoft.Data.Sqlite y EF Core. No convertir recomendaciones condicionadas (p.ej. WAL) en cambios automáticos.
