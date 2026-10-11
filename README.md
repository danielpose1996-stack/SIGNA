# AulaRegistro · Gestor Académico Docente

Aplicación de escritorio local para materias, estudiantes, notas y asistencias. Se abre sin cuenta ni inicio de sesión. Está desarrollada en C#/.NET 10, Avalonia UI y SQLite, con capas Domain, Application, Infrastructure y Desktop.

## Ejecutar en Windows

Con el SDK de .NET 10 instalado, desde PowerShell en la carpeta del proyecto:

```powershell
.\Iniciar.cmd
```

El iniciador compila antes de abrir; por eso puede tardar varios segundos. Para ejecutar una compilación ya existente directamente:

```powershell
dotnet run --project src\GestorDocente.Desktop --no-build --no-restore
```

Si existe un SDK portátil en `.tools/dotnet`, el iniciador lo utiliza automáticamente. La base de producción se guarda en la carpeta `AulaRegistro` de datos locales del usuario del sistema operativo. `GESTOR_DOCENTE_DATA_DIR` permite elegir otro directorio local. Evita carpetas compartidas de red o sincronizadas para la base activa.

## Uso académico

Primero crea un período académico, por ejemplo `2026-1`, y luego una asignatura con código, nombre y escuela. Importa un `.xlsx`: solo se guardan documento y nombre de los estudiantes. En cada uno de los tres cortes configura actividades principales cuyos porcentajes suman 100 %. Cada actividad puede usar una nota directa o el promedio simple de subactividades. Las notas válidas son de 0,0 a 5,0. Las asistencias admiten hasta nueve semanas por corte, de clase o actividades, con estados P/A/J/T; las celdas sin diligenciar no cuentan como ausencias.

En **Respaldos** puedes crear un archivo local cifrado `.aulabk` y restaurarlo con la contraseña que elijas. Consérvala: la aplicación no la almacena ni puede recuperarla. Cierra otras instancias antes de restaurar. Antes de reemplazar una base se verifica el respaldo y se crea una copia preventiva cifrada en `Recovery` junto a los datos locales. Puedes subir manualmente el `.aulabk` a tu propio Drive. La conexión automática a Google Drive aún no está implementada.

La base SQLite activa usa los permisos de la cuenta del sistema operativo y **no está cifrada**; los archivos de respaldo sí lo están. Mantén protegida la cuenta y el disco del equipo. No incluyas bases, archivos `.aulabk` ni listados de alumnos en commits o incidencias públicas.

## Desarrollo y pruebas

```powershell
dotnet test GestorDocente.slnx
```

Las pruebas usan datos ficticios y bases temporales. Los porcentajes de J/T y el peso de cada corte en una nota final siguen pendientes de definición; el programa no inventa esos cálculos.
