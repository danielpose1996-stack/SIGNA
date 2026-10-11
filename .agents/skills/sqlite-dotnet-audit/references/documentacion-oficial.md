# Referencias verificables — SQLite + .NET

Documentación primaria consultada para la skill. Al aplicar una recomendación, enlazar a la sección que realmente la respalda; si algo no está respaldado, marcarlo como criterio de ingeniería o hipótesis.

1. SQLite — transacciones, varios lectores/un escritor, bloqueos y rollback: https://www.sqlite.org/lang_transaction.html
2. SQLite — Write-Ahead Logging, limitaciones con sistemas de archivos de red y recuperación: https://www.sqlite.org/wal.html
3. SQLite — respaldos de una base en uso mediante Online Backup API: https://www.sqlite.org/backup.html
4. SQLite — claves foráneas y comportamiento de su aplicación: https://www.sqlite.org/foreignkeys.html
5. SQLite — PRAGMA foreign_keys, integrity_check, foreign_key_check, journal_mode, synchronous, optimize: https://www.sqlite.org/pragma.html
6. SQLite — diagnóstico de consultas con EXPLAIN QUERY PLAN: https://www.sqlite.org/eqp.html
7. Microsoft — límites de I/O asincrónico en Microsoft.Data.Sqlite: https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async
8. Microsoft — opciones de cadenas de conexión, Foreign Keys, Default Timeout: https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings
9. Microsoft — respaldo en línea con SqliteConnection.BackupDatabase (bloqueo de escritores durante operación): https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup
10. Microsoft — DbContext es unidad de trabajo de vida corta y no es thread-safe: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
11. Microsoft — consultas eficientes, proyecciones, tracking, N+1: https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying
12. Microsoft — limitaciones del proveedor SQLite EF Core y migraciones: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations
13. Microsoft — aplicación de migraciones EF Core, protección y riesgos: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
14. Agent Skills — formato SKILL.md (frontmatter y referencias): https://agentskills.io/specification

## Matices que no deben omitirse

- Activar WAL es una decisión de arquitectura: no hacerlo sobre almacenamiento remoto compartido y no asumir que soluciona escritores concurrentes.
- Con WAL y `synchronous=NORMAL`, puede perderse la última transacción tras apagón/corte de energía; no establecer ese PRAGMA sin evaluar la durabilidad requerida.
- El backup mediante `BackupDatabase` es consistente, pero puede bloquear escrituras durante la operación: medir con base de prueba y comunicarlo al usuario.
- `DbContext` no es thread-safe. Si se traslada una operación a un hilo de trabajo, crear el contexto para esa unidad de trabajo; no trasladar un contexto compartido con la UI.
- SQLite no soporta `decimal` de forma nativa como algunos motores SQL; evitar cambios automáticos de precisión en notas.
- `PRAGMA integrity_check` NO verifica errores de claves foráneas; ejecutar `PRAGMA foreign_key_check` por separado.
