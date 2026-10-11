# AGENTS.md — Gestor Académico Docente

## Alcance y fuentes de verdad
Este repositorio es una aplicación de escritorio local-first para gestión académica de docentes. Antes de cualquier implementación, leer `.specify/memory/constitution.md`, el documento `specs/README.md` y la especificación aprobada del módulo afectado. Si faltan criterios de aceptación, primero redactarlos; no inventar políticas institucionales.

## Stack aprobado
- C# con .NET 10, Avalonia UI (desktop multiplataforma), patrón MVVM, SQLite.
- Git y GitHub para versionado. Pruebas automatizadas de dominio, aplicación e integración.
- El sistema opera sin internet; Google Drive solo se utiliza para respaldos manuales opcionales.
- No introducir ASP.NET, WPF, servicios remotos, telemetría, microservicios ni servicios SaaS sin una decisión arquitectónica aprobada.

## Flujo obligatorio: SDD + TDD
1. Leer constitución y especificación; identificar ambigüedades y riesgos.
2. Proponer plan corto, archivos a modificar, casos de prueba y decisiones técnicas.
3. Definir criterios de aceptación comprobables antes de implementar.
4. Implementar en incrementos pequeños; primero pruebas de dominio y luego producción.
5. Ejecutar `dotnet build` y `dotnet test` cuando la solución exista; reportar resultados exactos, no inventados.
6. Presentar diff, riesgos, limitaciones y pasos de verificación visual en Windows.
7. Nunca ampliar alcance silenciosamente. No sobrescribir cambios existentes del usuario.

## Diseño
- Estética universitaria sobria; interfaz clara, profesional, sin sobrecarga, alineada con las maquetas aprobadas.
- Tabla con rayado de cebra, edición clara y navegación accesible por teclado.
- La construcción de rúbricas debe realizarse en ventana modal, no en panel lateral permanente.
- Probar visualmente tamaño normal, redimensionado, escala de pantalla y estados sin datos / error.
- Usar una skill de UX/UI solo cuando se haya revisado y aprobado su procedencia; no asumir que una maqueta es código ejecutable.

## Seguridad y datos
- Usar datos ficticios en desarrollo y en capturas. Nunca confirmar ni publicar datos reales de estudiantes sin permiso.
- No almacenar secretos, contraseñas, tokens OAuth ni bases `.db` en Git.
- Revisar licencias, manifiestos, scripts y dependencias de terceros antes de instalar.
- No ejecutar automáticamente scripts remotos, hooks, MCP desconocidos o acciones destructivas.
- Validar entradas, integridad referencial, transacciones y restauraciones.
- No copiar en caliente un archivo SQLite abierto para hacer respaldos: usar API de backup consistente.
- Cifrar los archivos destinados a Google Drive antes de subirlos. Usar OAuth de mínimo privilegio.

## Calidad
- Separar UI, lógica académica, persistencia y servicios externos.
- Evitar reglas de calificaciones hardcodeadas sin aprobación (pesos, redondeo, tardanzas y justificaciones).
- No mezclar acceso a SQLite ni llamadas a Drive en los ViewModels.
- Escribir tests de bordes: pesos, notas fuera de rango, grupos con varias actividades, semana sin clase, Excel inválido, backup corrupto.
- Preferir librerías mantenidas, versiones controladas y actualizaciones revisadas.

## Política de skills
- Prioridad a skills oficiales de `github.com/dotnet/skills` (C#, pruebas y EF Core).
- Referencias Avalonia y UX comunitarias: revisar archivos `SKILL.md`, scripts y dependencias; no instalar automáticamente.
- Activar solo la skill pertinente a la tarea, no toda la colección simultáneamente.

## Versionado del proyecto
- Repositorio oficial: https://github.com/danielpose1996-stack/aula-registro.git.
- Utilizar `origin` y la rama `main`, conservando el historial remoto y los archivos locales.
- Escribir los mensajes de commit en español, por solicitud explícita del usuario.
- Solicitar autorización explícita antes del primer push; los siguientes envíos deben respetar el alcance autorizado de cada solicitud.
- Revisar el contenido de los commits y las exclusiones antes de publicar; no usar push forzado para reemplazar el historial.
