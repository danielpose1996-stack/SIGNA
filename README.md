# Gestor Académico Docente

Aplicación de escritorio para administrar materias, grupos, estudiantes, notas, rúbricas y asistencias. Su diseño parte de las especificaciones SDD del paquete entregado por el usuario.

**Estado actual:** estructura documental preparada; constitución y especificaciones pendientes de aprobación. No hay aplicación ejecutable, solución .NET ni módulos implementados.

## Tecnología prevista

C# / .NET 10, Avalonia con MVVM y SQLite, según el paquete original. Funcionamiento local sin conexión; Google Drive únicamente para respaldos manuales opcionales. Las versiones concretas de Avalonia y de las demás dependencias se revisarán y fijarán antes de instalarlas.

## Estructura

```text
.
├── AGENTS.md                         # Instrucciones originales para el proyecto
├── LEEME-PRIMERO.md                  # Guía original del paquete
├── .specify/memory/constitution.md   # Constitución original en borrador
├── .agents/skills/                   # Política de skills; sin instalaciones
├── design-references/               # Espacio para maquetas aprobadas
├── docs/
│   ├── arquitectura.md              # Capas y dependencias propuestas
│   ├── plan-desarrollo.md            # Orden, validaciones y entregables
│   ├── decisiones-pendientes.md      # Reglas que necesitan definición
│   └── decisions/                   # Registro de decisiones arquitectónicas
├── specs/
│   ├── 001-materias/spec.md
│   ├── 002-importacion-estudiantes/spec.md
│   ├── 003-notas/spec.md
│   ├── 004-rubricas/spec.md
│   ├── 005-asistencias/spec.md
│   ├── 006-informes/spec.md
│   └── 007-respaldos/spec.md
├── src/
│   ├── GestorDocente.Domain/
│   ├── GestorDocente.Application/
│   ├── GestorDocente.Infrastructure/
│   └── GestorDocente.Desktop/
└── tests/
    ├── GestorDocente.Domain.Tests/
    ├── GestorDocente.Application.Tests/
    ├── GestorDocente.Infrastructure.Tests/
    └── GestorDocente.Acceptance.Tests/
```

Las carpetas `src/` y `tests/` son reservas de estructura, no proyectos compilables. Infraestructura contendrá persistencia y adaptadores externos sin mezclarlos con la interfaz.

## Ruta de revisión

1. Revisar la constitución y `docs/decisiones-pendientes.md`.
2. Revisar las propuestas de arquitectura y desarrollo.
3. Aprobar la especificación y el diseño del primer incremento: materias y grupos.
4. Revisar dependencias, preparar .NET 10 y crear la solución y proyectos.
5. Implementar cada incremento con pruebas proporcionales al riesgo, compilación y revisión visual.

No es necesario resolver todas las reglas de módulos futuros para comenzar un módulo independiente. Ninguna regla pendiente se utilizará como valor predeterminado sin aprobación.

## Verificación de esta preparación

Se conservan los siete archivos originales del ZIP, comprobados mediante SHA-256. Los documentos añadidos son propuestas y no cambian el estado de aprobación de la constitución. Se inicializó un repositorio Git local en la rama `main`, sin commits ni remoto. `dotnet` no estaba disponible en el PATH. No se han instalado herramientas, paquetes ni skills, ni se ha publicado el proyecto en GitHub.

Las pruebas y la compilación se ejecutarán cuando exista la solución. Los fixtures y las capturas deberán usar únicamente datos ficticios.
