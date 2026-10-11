# Pruebas previstas

Las carpetas reservan proyectos futuros; no existen tests ejecutables todavía.

| Proyecto | Propósito |
| --- | --- |
| GestorDocente.Domain.Tests | Reglas y cálculos aprobados, sin UI ni base de datos |
| GestorDocente.Application.Tests | Casos de uso y coordinación mediante dobles de contratos |
| GestorDocente.Infrastructure.Tests | SQLite, migraciones, Excel, PDF y respaldo con datos ficticios |
| GestorDocente.Acceptance.Tests | Escenarios aprobados y recorridos de módulos |

Los IDs de criterios de aceptación de `specs/` permitirán relacionar requisitos con pruebas. Seleccionar y auditar el framework antes de instalarlo. Usar archivos temporales, limpiar después de cada prueba y evitar datos reales, credenciales o llamadas a cuentas reales en las pruebas automáticas.
