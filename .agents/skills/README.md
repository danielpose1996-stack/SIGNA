# Skills del proyecto

Codex reconoce skills mediante `.agents/skills/<nombre>/SKILL.md`.

Skill activada por solicitud del docente: `sqlite-dotnet-audit`, versión 1.0.0, del paquete `aula-registro-sqlite-skill.zip`. Se revisaron SKILL.md y sus referencias antes de usarla. Contiene solo Markdown, sin scripts, instaladores ni dependencias ejecutables. Es una skill personalizada de AulaRegistro, no una skill oficial de Microsoft o SQLite. Se aplica primero al diagnóstico; sus recomendaciones de producción requieren alcance aprobado mediante SDD/TDD. Primera auditoría: `docs/audits/sqlite-audit-2026-10-11.md`.

Primero revisar procedencia, `SKILL.md`, scripts, versiones y licencias. La colección oficial .NET está en https://github.com/dotnet/skills.

La skill comunitaria de UX está en https://github.com/nextlevelbuilder/ui-ux-pro-max-skill; sus scripts no se deben ejecutar sin revisar dependencias y permisos.
