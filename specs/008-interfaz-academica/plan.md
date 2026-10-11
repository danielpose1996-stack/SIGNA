# Plan técnico

1. Auditar SDK Microsoft y NuGet; fijar versiones. SDK portable local, sin modificar configuración del sistema ni instalar skills.
2. Crear solución y proyectos en `src/` y `tests/` existentes. Añadir pruebas de reglas antes de producción.
3. Domain: modelo de workspace, validadores y calculadoras. Application: contrato, servicio de cambios atómicos y fábrica de demostración. Infrastructure: almacenamiento SQLite exclusivo de demo.
4. Desktop: objetos observables/comandos, ViewModels separados, shell y estilos AXAML, dos vistas retenidas, tablas dinámicas con bindings, editor/configuración y evaluación modal.
5. Verificar compilación/pruebas, abrir app, capturar evidencia con Computer Use y corregir diseño. Documentar ejecución y limitaciones.

Archivos: `.csproj`, `.slnx`, `global.json`, configuración común de dependencias; modelos/validaciones/calculadoras; contrato/servicio/fábrica; repositorio SQLite; App/MainWindow, vistas/ventanas, estilos/iconos y ViewModels; tests; documentación/evidencia. No implementar módulos ajenos al alcance.
