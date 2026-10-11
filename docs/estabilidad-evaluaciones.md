# Corrección 013 — Creación y guardado de evaluaciones

## Causas comprobadas

1. ConfigurationViewModel exponía List<EvaluationActivity> como fuente de la tabla y emitía PropertyChanged con la misma instancia después de agregar. No había eventos incrementales de colección para que DataGrid reflejara las nuevas filas. Una prueba previa a la corrección confirmó esta ausencia.
2. WorkspaceService confirmaba la escritura y notificaba Changed. MainViewModel reemplazaba las filas antes de avisar a NotesView del nuevo esquema de columnas. DataGrid reutilizaba una columna antigua (por ejemplo, t1) con un nuevo diccionario de celdas. Se reprodujo KeyNotFoundException en NotesView.BuildColumns → MainViewModel.Rebuild → WorkspaceService.Change, después de guardar. El controlador async void no capturaba esa excepción; esto podía cerrar el proceso.
3. La preparación del workspace y la escritura SQLite eran sincrónicas en el hilo de interfaz. Una espera por almacenamiento podía bloquearlo. El manejo anterior no cubría permisos ni errores inesperados y no distinguía un fallo de escritura de un fallo de actualización posterior al commit.

## Correcciones

- Colección observable por actividad principal, con altas/bajas incrementales que conservan la selección. El borrador sigue aislado; agregar subactividades no escribe en disco ni reconstruye las tablas principales.
- Confirmación de la edición de nombres/porcentajes antes de agregar o guardar.
- Actualización ordenada: retirar las filas anteriores, actualizar columnas y publicar las nuevas filas en bloque. Se evita tanto el acceso a IDs antiguos como una notificación por cada estudiante.
- ConfigureGroupsAsync prepara un workspace separado y persiste en un worker; Current y Changed se publican después del commit en el contexto del llamador. No se modifica la capa SQLite, su transacción, revisión optimista ni el esquema.
- Mientras se guarda: indicador visible, controles deshabilitados, doble envío bloqueado y cierre del diálogo pospuesto. Otras modificaciones al mismo workspace se rechazan con un mensaje explícito durante esa operación.
- Errores visibles con detalles técnicos desplegables. Un fallo conserva el borrador y permite reintentar; el workspace actual solo cambia tras una escritura correcta. Si falla un suscriptor después de confirmar, el mensaje indica que los datos sí se guardaron y que falló la actualización de la pantalla.
- Confirmación de modalidad con espacio suficiente para su texto y botones. Se mantienen las notas directas, promedios simples, retirados recuperables y validación del 100 %.

## Archivos de este incremento

- src/GestorDocente.Application/WorkspaceService.cs
- src/GestorDocente.Desktop/ViewModels/ConfigurationViewModel.cs
- src/GestorDocente.Desktop/ViewModels/MainViewModel.cs
- src/GestorDocente.Desktop/ViewModels/ObservableRangeCollection.cs (nuevo)
- src/GestorDocente.Desktop/Views/ConfigurationWindow.axaml y .axaml.cs
- tests/GestorDocente.Acceptance.Tests/EvaluationStabilityTests.cs (nuevo)
- specs/013-estabilidad-evaluaciones/spec.md y este documento (nuevos)

Sin nuevas dependencias ni skills instaladas. No hay una skill específica de SQLite disponible; se reutiliza Microsoft.Data.Sqlite y la infraestructura existente.

## Pruebas y revisión

Dos regresiones fallaron antes de implementar: colección sin notificaciones y cierre por celda t1 inexistente. Se añadieron diez escenarios headless de aceptación: 30 altas sucesivas con layout y selección estable; cambio de columnas con estudiantes visibles; botón de guardar con SQLite y 100 alumnos, seguido de reapertura; conflicto de revisión SQLite; permisos y error inesperado; porcentajes inválidos; confirmación de modalidad y doble envío; escritura lenta con interfaz capaz de procesar eventos, cierre protegido y publicación en el hilo de UI; diagnóstico correcto de un fallo posterior al commit.

Las bases de prueba son temporales y contienen datos ficticios. Las pruebas de ventanas Avalonia headless verifican eventos y layout; no sustituyen una revisión visual manual en Windows.

Validación del incremento: compilación completa de la solución y del ejecutable habitual, ambas con 0 errores y 0 advertencias. Suite completa: 85 pruebas aprobadas, 0 fallidas y 0 omitidas (14 dominio, 26 aplicación, 17 infraestructura y 28 aceptación). No se utilizaron bases reales ni se controló la interfaz del docente.

Ejecutar pruebas:

```powershell
.\.tools\dotnet\dotnet.exe test GestorDocente.slnx
```

Para probar manualmente, cerrar la versión anterior y ejecutar:

```powershell
cd "C:\Users\danie\Documents\control de notas"
.\Iniciar.cmd
```

Abrir una materia, crear Talleres y su porcentaje, habilitar subactividades y agregar tres. Editar sus nombres, completar los porcentajes hasta 100 %, guardar, ingresar notas y reabrir la materia. Verificar también que un porcentaje incorrecto mantenga abierto el diálogo con su mensaje de validación. La revisión visual sigue a cargo del docente, según su preferencia.
