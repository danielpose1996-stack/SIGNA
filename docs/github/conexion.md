# Conexión del proyecto con GitHub

Configuración solicitada por el usuario el 2026-10-11:

- Repositorio oficial: [danielpose1996-stack/aula-registro](https://github.com/danielpose1996-stack/aula-registro).
- Remoto `origin`: `https://github.com/danielpose1996-stack/aula-registro.git`.
- Rama local: `main`, con seguimiento de `origin/main`.
- Base de historial: commit remoto `6fbb8187a4145c8b330707dc687873246ad9e41d` (`Initial commit`).

## Integración sin pérdida de archivos

El proyecto local no tenía commits. Se obtuvo el historial remoto y se estableció como base de `main` mediante un reset de índice (`--mixed`), que conserva los archivos de trabajo. No se utilizó reset `--hard`, checkout de archivos locales, eliminación ni push forzado.

El `README.md` local se conserva íntegro en la raíz. El README original de GitHub se conserva íntegro en `docs/github/README.md`. La licencia MIT del remoto se incorpora sin modificaciones como `LICENSE` en la raíz y sigue formando parte del historial remoto. La documentación local describe la preparación SDD inicial; para el alcance implementado, consultar `specs/008-interfaz-academica/`.

Antes de integrar se creó una copia local de los 103 archivos no ignorados y un manifiesto SHA-256 dentro de `.git/local-integration-backup-20261011/`. Esta copia no forma parte de los archivos que se subirán. Se amplía el `.gitignore` y se añaden instrucciones de versionado a `AGENTS.md`, conservando íntegro su contenido previo. Los demás archivos existentes se conservan sin modificaciones.

## Archivos protegidos

El `.gitignore` excluye herramientas locales, `bin/obj`, la base de demostración, archivos de bases de datos y sus diarios, claves privadas, archivos habituales de tokens/credenciales, `.env`, respaldos/exportaciones locales y resultados de pruebas con rutas del equipo. Las skills y la memoria SDD permanecen versionables. Los archivos de bloqueo de NuGet también permanecen versionables.

Las exclusiones no sustituyen la revisión previa de lo que se incluye en un commit. No guardar secretos o datos personales en archivos de código o configuración versionables. Los recursos de diseño suministrados por el usuario sí forman parte del proyecto.

## Primer envío y trabajo posterior

El primer push permanece pendiente de autorización explícita del usuario. Se prepara un commit local encima del historial remoto para que el envío pueda ser normal, sin forzar ni reemplazar ese historial.

Tras la autorización, comprobar nuevamente el remoto y enviar `main` a `origin` con `git push -u origin main`. Si el remoto recibió nuevos cambios, integrarlos conservando los archivos y revisar otra vez antes del envío.

Este será el repositorio de referencia para los siguientes incrementos. Usar `git status`, revisar cambios e ignorados y conservar los criterios SDD y las pruebas antes de gestionar commits o publicaciones dentro del alcance autorizado de cada solicitud.

Los mensajes de commit deben escribirse en español, según la preferencia explícita del usuario.
