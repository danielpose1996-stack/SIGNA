# 013 — Rendimiento y estabilidad de la configuración de evaluaciones

Solicitud aprobada: corregir retrasos al crear actividades/subactividades y el cierre al guardar, sin alterar las reglas de evaluación de 012 ni otros módulos.

## Criterios comprobables

- EST-01: agregar/eliminar subactividades notifica un cambio incremental de colección; mantiene la selección y edición de las filas existentes. No escribe SQLite ni reconstruye las tablas principales durante la edición del borrador.
- EST-02: guardar con estudiantes y columnas existentes admite nuevas actividades, cambios de modalidad y eliminación de columnas sin utilizar celdas del esquema anterior. La aplicación y sus ventanas permanecen operativas.
- EST-03: la escritura se ejecuta fuera del hilo de interfaz; una sola operación en curso, controles deshabilitados y mensaje de progreso. Cambios publicados y notificaciones MVVM vuelven al hilo de interfaz después de confirmar la transacción.
- EST-04: errores de validación, permisos y SQLite conservan el borrador y el workspace anterior, muestran la causa comprensible y permiten reintentar. Errores inesperados también se muestran con sus detalles técnicos; no se capturan silenciosamente ni se anuncian como guardados.
- EST-05: comprobación automatizada de agregados sucesivos, persistencia/reapertura SQLite, guardado fallido, doble envío y capacidad de procesar eventos de interfaz durante una escritura lenta controlada.

## Plan y límites

Editar ConfigurationViewModel/ConfigurationWindow para colección observable y guardado asincrónico; separar preparación, persistencia y publicación del cambio en WorkspaceService; actualizar las columnas y filas de MainViewModel/NotesView de forma coherente. Pruebas headless de eventos Avalonia y pruebas de persistencia con bases temporales ficticias. Sin nuevas dependencias ni cambios de esquema, cálculos, importación, respaldos o lanzamiento. La revisión visual del docente sigue siendo manual.
