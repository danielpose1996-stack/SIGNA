# Actualizaciones de SIGNA

SIGNA incorpora Velopack desde **1.6.0**. La edición **1.6.1** mejora el botón de búsqueda y limita el soporte futuro a Windows 10/11 x64, macOS 15+ Intel/Apple Silicon y Ubuntu 24.04 LTS x64/ARM64. Las ediciones hasta **1.5.0** necesitan instalar una vez la edición nueva para recibir actualizaciones internas posteriores.

## Para el docente

Al abrir SIGNA se comprueba en segundo plano si hay una versión estable nueva en el [repositorio oficial](https://github.com/danielpose1996-stack/SIGNA/releases/latest). No se envían materias, estudiantes, notas, preferencias ni respaldos. Sin internet puedes trabajar normalmente; no aparece un diálogo que impida abrir la aplicación.

También puedes pulsar **Buscar actualizaciones**, en la parte inferior del menú lateral. El diálogo muestra la versión actual, la disponible y el resultado de la comprobación. Si existe una actualización instalable, **Actualizar SIGNA** descarga el paquete y muestra su progreso. SIGNA comprueba su tamaño y SHA-256, verifica los datos locales y prepara la instalación y el reinicio. La descarga solo empieza por acción del docente. Puedes cancelarla antes del reinicio.

Guarda las notas pendientes, termina las operaciones y cierra otras instancias de SIGNA antes de actualizar. Si hay un problema de conexión, descarga, integridad o cambios sin guardar, se informa el error y puedes continuar con la versión actual. Un paquete descargado no se instala automáticamente en el siguiente arranque: requiere volver a pasar las verificaciones.

## Primera instalación de la edición actualizable

Las versiones hasta 1.5.0 se distribuyeron con otro sistema de empaquetado. Necesitan instalar **una vez** la nueva edición desde Releases. No pueden recibir Velopack mediante un actualizador que todavía no tienen.

1. Crea un respaldo local en **Respaldos**, fuera de la carpeta del programa. Si usas un archivo cifrado de una edición anterior, conserva su contraseña original.
2. Cierra SIGNA. Descarga el instalador de tu sistema y arquitectura desde la página oficial. Comprueba su SHA-256.
3. En Windows abre el nuevo Setup, revisa la licencia MIT y selecciona una carpeta de instalación. El asistente delega en Velopack y admite una carpeta local vacía o una instalación SIGNA compatible, con permisos de escritura; rechaza archivos ajenos, datos y respaldos dentro del destino. Instala por usuario sin requerir desinstalar primero la edición anterior. Abre el acceso nuevo. Si eliges otra carpeta, la instalación anterior puede permanecer en su ubicación; evita abrir simultáneamente ambas ediciones. La ubicación de SQLite y los respaldos no depende de la carpeta del programa.
4. En macOS arrastra el nuevo SIGNA.app del DMG a Aplicaciones, confirma reemplazar el programa y expulsa el DMG. Ejecuta SIGNA desde su ubicación definitiva, con permiso para reemplazar el bundle. El sistema puede solicitar autorización al actualizar una aplicación situada en `/Applications`.
5. En Ubuntu, para usar la actualización integrada, descarga la AppImage de tu arquitectura, guárdala en una carpeta propia estable y escribible, dale permiso de ejecución (`chmod +x SIGNA-1.6.1-linux-x64.AppImage`) y ábrela. Conserva el archivo fuera de carpetas de descargas temporales. Requiere las bibliotecas de .NET/Avalonia y soporte AppImage/FUSE de la distribución. En Wayland necesita XWayland.
6. Comprueba que tus materias están disponibles en la nueva edición. Las futuras versiones estables podrán instalarse con **Actualizar SIGNA**, sin repetir la instalación inicial.

En Ubuntu se conserva el paquete **.deb**, que se actualiza descargando la nueva edición y usando apt. La actualización interna corresponde a **AppImage**; SIGNA no sobrescribe archivos administrados por apt en `/usr`. Puedes abrir la página oficial de descargas desde la ventana de actualizaciones. Las dos ediciones comparten la misma ubicación de datos si se ejecutan con la misma cuenta; evita abrirlas simultáneamente. Desde 1.6.1 no se generan RPM ni paquetes Arch/Omarchy y no se ofrece soporte Debian. Las Releases históricas no se borran.

Las distribuciones continúan sin firma oficial de Windows ni Apple Developer ID/notarización. Velopack y SHA-256 no sustituyen esos certificados. macOS conserva firma ad hoc de integridad y puede requerir autorizar la app verificada conforme al procedimiento de Apple.

El instalador Windows mantiene el asistente de licencia MIT y carpeta, mientras Velopack administra actualizaciones/desinstalación. Desde una instalación Velopack 1.6.0 puedes recibir 1.6.1 por **Buscar actualizaciones → Actualizar SIGNA**, sin desinstalar manualmente. **Un commit en main no activa la actualización:** deben publicarse una Release estable superior, su feed y su paquete completo de cada canal. Las versiones de parche como 1.6.1 sirven para mejoras y correcciones compatibles; no es necesario subir la versión menor en cada cambio.

## Conservación de información

No cambian el esquema de SQLite ni la ubicación histórica de los datos: `LocalApplicationData/AulaRegistro/aula-registro.db`, o el directorio configurado explícitamente mediante `GESTOR_DOCENTE_DATA_DIR`. Incluye semestres, materias, estudiantes y contactos, cortes y ponderaciones, actividades, subactividades, notas, asistencias y preferencias de interfaz/perfil.

El actualizador sustituye **archivos del programa**, sin restaurar, recrear o migrar la base, mover los respaldos ni cambiar sus contraseñas. Antes del reinicio se valida SQLite en modo de solo lectura, incluidas las relaciones y los agregados académicos. La comprobación admite datos confirmados en WAL. Si detecta una base o un respaldo dentro de la carpeta reemplazable del programa, bloquea la actualización e informa el motivo: conserva esos archivos fuera del programa antes de continuar.

Los respaldos `.aulabk` permanecen donde los guardaste, tanto los nuevos sin contraseña como los cifrados anteriores. La base activa sigue sin cifrar. La limpieza local solo reinicializa el catálogo y sus preferencias; no elimina copias ni archivos del actualizador. Mantén respaldos periódicos y conserva la contraseña de los archivos antiguos: el actualizador no sustituye ese cuidado.

## Publicación y arquitectura

- `Application`: contratos del actualizador, guardas y reglas de versiones/canales; sin dependencia de Velopack.
- `Infrastructure`: `GithubSource` público, solo versiones estables superiores, sin token ni downgrade. Descarga y verificación SHA-256, también para paquetes reutilizados de caché. Guardas SQLite separadas de la interfaz.
- `Desktop`: Velopack se inicializa como primera instrucción de `Main`; aviso, diálogo MVVM, comprobación inicial no bloqueante y cierre controlado después del handoff. La demostración y las pruebas Headless no consultan la red.
- `eng` y el workflow de Release: SDK y CLI fijados a **1.2.161**; compilación y empaquetado en cada sistema nativo. Windows usa Setup Velopack; macOS conserva DMG con el bundle preparado por Velopack; Ubuntu conserva AppImage actualizable y paquete .deb para apt.

Cada Release debe publicar los instaladores, el paquete completo `.nupkg` y `releases.<canal>.json` de **cada** canal: `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`. No borres ni renombres los feeds o paquetes que el workflow genera. `collect-velopack.py` comprueba versión, identidad, canal, arquitectura, rutas, exclusión de datos/secretos y SHA-256; `release-manifest.py` exige el conjunto completo antes de publicar. Se generan siete distribuciones y diez archivos de actualización, más `SHA256SUMS.txt` e `INSTALACION.md`.

Para preparar una versión: cambia `Directory.Build.props`, actualiza las notas `eng/RELEASE.md`, compila y prueba, y ejecuta el workflow **Instaladores y Release SIGNA** con la misma versión, únicamente después de autorizar su publicación. El workflow crea primero un borrador, comprueba los hashes descargando los archivos subidos y solo entonces la marca como estable. Las versiones preliminares y los borradores no se ofrecen al docente. Conserva las Releases históricas.

La validación ejecuta las suites de la aplicación y las pruebas de empaquetado en los runners correspondientes, con datos SQLite ficticios y caché corrupta. Windows y Ubuntu comprueban instalación/desinstalación en entornos efímeros; macOS comprueba el bundle y el DMG, y Ubuntu la AppImage y el paquete .deb. Las capturas Headless permiten revisar la presentación, pero no sustituyen un ciclo real de actualización entre dos Releases instaladas. La instalación/reinicio y la transición desde 1.5.0 requieren verificación manual en cada plataforma. No se ejecutan sobre el perfil real del docente durante el desarrollo.

Referencias: [integración Velopack](https://docs.velopack.io/integrating/overview), [Linux](https://docs.velopack.io/packaging/operating-systems/linux), [macOS](https://docs.velopack.io/packaging/operating-systems/macos), [código fijado a 1.2.161](https://github.com/velopack/velopack/tree/1.2.161).
