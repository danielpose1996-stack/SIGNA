# Actualizaciones de SIGNA

La versión **1.6.0**, disponible en [GitHub Releases](https://github.com/danielpose1996-stack/SIGNA/releases/tag/v1.6.0), incorpora Velopack 1.2.161. Las ediciones hasta **1.5.0** todavía no contienen este actualizador: necesitan instalar una vez la edición nueva para recibir actualizaciones internas posteriores.

## Para el docente

Al abrir SIGNA se comprueba en segundo plano si hay una versión estable nueva en el [repositorio oficial](https://github.com/danielpose1996-stack/SIGNA/releases/latest). No se envían materias, estudiantes, notas, preferencias ni respaldos. Sin internet puedes trabajar normalmente; no aparece un diálogo que impida abrir la aplicación.

También puedes pulsar **Buscar actualizaciones**, en la parte inferior del menú lateral. El diálogo muestra la versión actual, la disponible y el resultado de la comprobación. Si existe una actualización instalable, **Actualizar SIGNA** descarga el paquete y muestra su progreso. SIGNA comprueba su tamaño y SHA-256, verifica los datos locales y prepara la instalación y el reinicio. La descarga solo empieza por acción del docente. Puedes cancelarla antes del reinicio.

Guarda las notas pendientes, termina las operaciones y cierra otras instancias de SIGNA antes de actualizar. Si hay un problema de conexión, descarga, integridad o cambios sin guardar, se informa el error y puedes continuar con la versión actual. Un paquete descargado no se instala automáticamente en el siguiente arranque: requiere volver a pasar las verificaciones.

## Primera instalación de la edición actualizable

Las versiones hasta 1.5.0 se distribuyeron con otro sistema de empaquetado. Necesitan instalar **una vez** la nueva edición desde Releases. No pueden recibir Velopack mediante un actualizador que todavía no tienen.

1. Crea un respaldo local cifrado en **Respaldos**, fuera de la carpeta del programa, y conserva la contraseña.
2. Cierra SIGNA. Descarga el instalador de tu sistema y arquitectura desde la página oficial. Comprueba su SHA-256.
3. En Windows abre el nuevo Setup; instala por usuario sin requerir desinstalar primero la edición anterior. Abre el acceso nuevo. La ruta del programa cambia de la utilizada por Inno Setup; puede quedar un acceso antiguo. Evita abrir simultáneamente ambas ediciones.
4. En macOS arrastra el nuevo SIGNA.app del DMG a Aplicaciones, confirma reemplazar el programa y expulsa el DMG. Ejecuta SIGNA desde su ubicación definitiva, con permiso para reemplazar el bundle. El sistema puede solicitar autorización al actualizar una aplicación situada en `/Applications`.
5. En Linux, para usar la actualización integrada, descarga la AppImage de tu arquitectura, guárdala en una carpeta propia estable y escribible, dale permiso de ejecución (`chmod +x SIGNA-1.6.0-linux-x64.AppImage`) y ábrela. Conserva el archivo fuera de carpetas de descargas temporales. Requiere las bibliotecas de .NET/Avalonia y soporte AppImage/FUSE de la distribución. En Wayland necesita XWayland.
6. Comprueba que tus materias están disponibles en la nueva edición. Las futuras versiones estables podrán instalarse con **Actualizar SIGNA**, sin repetir la instalación inicial.

Los paquetes **DEB, RPM y Arch/Omarchy se conservan**. Se actualizan descargando la versión nueva y usando apt, dnf o pacman, respectivamente. SIGNA puede avisar de una versión nueva y abrir su página oficial, pero no sobrescribe archivos administrados por el gestor de paquetes. Para la actualización interna en esos equipos usa voluntariamente la edición AppImage; no necesitas migrar tus datos si ejecutas ambas ediciones con la misma cuenta y ubicación de datos. No abras dos ediciones a la vez.

Las distribuciones continúan sin firma oficial de Windows ni Apple Developer ID/notarización. Velopack y SHA-256 no sustituyen esos certificados. macOS conserva firma ad hoc de integridad y puede requerir autorizar la app verificada conforme al procedimiento de Apple.

## Conservación de información

No cambian el esquema de SQLite ni la ubicación histórica de los datos: `LocalApplicationData/AulaRegistro/aula-registro.db`, o el directorio configurado explícitamente mediante `GESTOR_DOCENTE_DATA_DIR`. Incluye semestres, materias, estudiantes y contactos, cortes y ponderaciones, actividades, subactividades, notas, asistencias y preferencias de interfaz/perfil.

El actualizador sustituye **archivos del programa**, sin restaurar, recrear o migrar la base, mover los respaldos ni cambiar sus contraseñas. Antes del reinicio se valida SQLite en modo de solo lectura, incluidas las relaciones y los agregados académicos. La comprobación admite datos confirmados en WAL. Si detecta una base o un respaldo dentro de la carpeta reemplazable del programa, bloquea la actualización e informa el motivo: conserva esos archivos fuera del programa antes de continuar.

Los respaldos cifrados `.aulabk` permanecen donde los guardaste. La base activa sigue sin cifrar; la actualización no cambia esa política. Mantén respaldos periódicos: ni el actualizador ni sus comprobaciones pueden proteger frente a fallos de disco, pérdida de contraseña o borrados externos.

## Publicación y arquitectura

- `Application`: contratos del actualizador, guardas y reglas de versiones/canales; sin dependencia de Velopack.
- `Infrastructure`: `GithubSource` público, solo versiones estables superiores, sin token ni downgrade. Descarga y verificación SHA-256, también para paquetes reutilizados de caché. Guardas SQLite separadas de la interfaz.
- `Desktop`: Velopack se inicializa como primera instrucción de `Main`; aviso, diálogo MVVM, comprobación inicial no bloqueante y cierre controlado después del handoff. La demostración y las pruebas Headless no consultan la red.
- `eng` y el workflow de Release: SDK y CLI fijados a **1.2.161**; compilación y empaquetado en cada sistema nativo. Windows usa Setup Velopack; macOS conserva DMG con el bundle preparado por Velopack; Linux añade AppImage y conserva DEB/RPM/Pacman.

Cada Release debe publicar los instaladores, el paquete completo `.nupkg` y `releases.<canal>.json` de **cada** canal: `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`. No borres ni renombres los feeds o paquetes que el workflow genera. `collect-velopack.py` comprueba versión, identidad, canal, arquitectura, rutas, exclusión de datos/secretos y SHA-256; `release-manifest.py` exige el conjunto completo antes de publicar. Se generan diez distribuciones y diez archivos de actualización, más `SHA256SUMS.txt` e `INSTALACION.md`.

Para preparar una versión: cambia `Directory.Build.props`, actualiza las notas `eng/RELEASE.md`, compila y prueba, y ejecuta el workflow **Instaladores y Release SIGNA** con la misma versión, únicamente después de autorizar su publicación. El workflow crea primero un borrador, comprueba los hashes descargando los archivos subidos y solo entonces la marca como estable. Las versiones preliminares y los borradores no se ofrecen al docente. Conserva las Releases históricas.

La validación incluye 427 pruebas de la aplicación en cada uno de los cinco sistemas/arquitecturas, seis pruebas de empaquetado, datos SQLite ficticios, caché corrupta y paquetes generados en sistemas nativos. Windows y Arch comprueban instalación/desinstalación en entornos efímeros; macOS comprueba el bundle y el DMG, y Linux la AppImage y sus paquetes. Las capturas Headless prueban distribución visual; no sustituyen un ciclo real de actualización entre dos Releases instaladas. La instalación/reinicio en Windows, macOS y AppImage y la transición desde 1.5.0 requieren verificación manual en cada plataforma. No se ejecutan sobre el perfil real del docente durante el desarrollo.

Referencias: [integración Velopack](https://docs.velopack.io/integrating/overview), [Linux](https://docs.velopack.io/packaging/operating-systems/linux), [macOS](https://docs.velopack.io/packaging/operating-systems/macos), [código fijado a 1.2.161](https://github.com/velopack/velopack/tree/1.2.161).
