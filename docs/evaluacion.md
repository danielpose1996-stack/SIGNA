# Actividades y calificaciones

1. Abre una materia y elige Corte 1, 2 o 3.
2. Pulsa **Actividades y porcentajes** y **Agregar actividad**.
3. Edita el nombre y porcentaje con doble clic. Puedes llamarla Talleres, Quices, Parcial, Proyecto o cualquier otro nombre. Los porcentajes del corte deben sumar 100 %.
4. Para una única nota, deja desmarcada **Usar subactividades**. La actividad tendrá una columna editable en la tabla de estudiantes.
5. Para varias notas, marca **Usar subactividades** y agrega/nombra las que necesites. Todas participan por igual en el promedio, sin porcentajes individuales.
6. Guarda la configuración e ingresa las notas entre 0,0 y 5,0. Pulsa Enter para guardar. El promedio y la nota del corte se actualizan automáticamente. Deja una celda vacía para una nota pendiente.

Ejemplo: Talleres 20 % con notas 4, 3 y 5 → promedio 4; Quices 20 % con 3 y 4 → promedio 3,5; Parcial directo 60 % con 4 → nota del corte 3,9.

Parcial funciona como cualquier actividad: puede tener una nota directa o varias subactividades. No hay rúbricas, criterios ni escalas de evaluación.

## Datos anteriores y cambios de modalidad

Las materias anteriores se actualizan al abrirse, conservando estudiantes, notas numéricas, asistencias e identificadores. Sus actividades individuales se mantienen como subactividades; todos los promedios pasan a ser simples, conforme a la regla nueva. Las notas calculadas previamente mediante criterios se conservan como notas numéricas editables.

Al pasar de nota directa a subactividades, la nota guardada se copia a la primera subactividad. Al pasar de subactividades a nota directa, se conserva su promedio completo; si faltan notas, queda pendiente. Las subactividades retiradas y sus notas se conservan en **Recuperar retirados**. El cambio solicita confirmación al guardar.

Los cortes se calculan por separado. Los pesos para combinar los tres cortes en una nota final del período aún están pendientes de definición.

## Archivos de esta entrega

- Dominio: AcademicModels, AcademicRules y GradeCalculator; modalidades explícitas, sin objetos ni cálculo de rúbricas.
- Aplicación: WorkspaceService y DemoWorkspaceFactory; migración a versión 2, conservación de notas y cambios de modalidad. AcademicReport adapta su proyección a ambas modalidades.
- SQLite: SqliteCatalogStore y SqliteWorkspaceStore; lectura de versiones anteriores y persistencia atómica de la versión nueva.
- Interfaz: ConfigurationViewModel/ConfigurationWindow, AcademicRows/GradeEditor, MainViewModel/MainWindow, NotesView, HomeView y RecoveryWindow/WeeksViewModel.
- Eliminados: RubricEditorViewModel, EvaluationViewModel, RubricWindow y EvaluationWindow, incluidos sus archivos AXAML y code-behind.
- Pruebas: AcademicRulesTests, WorkspaceTests, EvolutionTests, EvaluationModesTests, SqliteWorkspaceStoreTests, CatalogTests, AcademicLayoutTests y WindowTests.
- SDD: especificación 012, constitución y AGENTS actualizados; especificaciones antiguas de notas/rúbricas identificadas como históricas.

Sin nuevas dependencias ni skills; se utilizó la arquitectura C#/.NET/Avalonia existente. La revisión visual la realiza el docente manualmente por su indicación. No se generaron capturas de su base real.

## Validación y ejecución

Compilación completa: 0 errores y 0 advertencias. 75 pruebas correctas, sin fallos ni omisiones: 14 Domain, 26 Application, 17 Infrastructure y 18 Acceptance. Incluyen migración/reapertura SQLite, cambios de modalidad con conservación de notas, promedio simple, nota directa, 100 %, rango de notas, edición real de celdas, independencia de cortes y regresiones de importación/asistencia.

Se verificó el arranque real con datos aislados: ventana principal UNIPAZ y proceso Responding=True. No se afirma una inspección visual de las pantallas; la comprobación de nombres, tamaños y distribución queda para la revisión manual del docente.

Cerrar la aplicación anterior y ejecutar en PowerShell:

```powershell
cd "C:\Users\danie\Documents\control de notas"
.\Iniciar.cmd
```

Prueba sugerida: configurar Talleres 20 % y Quices 20 % con subactividades y Parcial 60 % con nota directa; ingresar las notas del ejemplo y verificar 3,90. Cambiar de corte y regresar, cerrar/reabrir la materia y verificar que las notas se mantienen. La eliminación del menú y modal anteriores es una desviación intencional de las referencias originales, autorizada en esta entrega.
