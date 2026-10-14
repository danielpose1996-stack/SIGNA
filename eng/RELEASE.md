**SIGNA 1.1.0 — Sistema Integral de Gestión de Notas y Asistencias**.

### Novedades

- **Editar semana:** permite cambiar el tipo de semana y agregar, modificar o quitar su fecha opcional, conservando las asistencias.
- **Numeración consecutiva:** las semanas visibles se reorganizan al retirar o recuperar una semana. Las claves internas permanecen estables para evitar asignaciones incorrectas de registros.
- **Perfil docente:** nombre personalizado en el menú lateral, con iconos y paletas existentes.
- **Color RGB:** selector libre de los canales rojo, verde y azul, con vista previa y contraste legible. Las preferencias se conservan al reiniciar y en respaldos locales cifrados.
- **Colores de rendimiento:** fondos suaves para las definitivas de corte y Nota consolidada; rojo por debajo de 3,0, amarillo por debajo de 4,0, verde por debajo de 4,6 y azul hasta 5,0. Se pueden activar o desactivar desde Personalizar apariencia, sin alterar notas ni cálculos.

La actualización conserva la ubicación de los datos y las preferencias anteriores. Antes de actualizar, crea una copia local cifrada desde Respaldos. Los instaladores no contienen materias ni estudiantes de prueba.

Incluye gestión de semestres/materias/estudiantes, importación Excel, actividades y subactividades, cortes configurables y nota consolidada, asistencias con porcentaje/gráficos/PDF, presencia masiva opcional, respaldos locales cifrados y personalización de apariencia. Funciona sin cuenta ni inicio de sesión.

### Descarga

- Windows x64: instalador `windows-x64-setup.exe`.
- Ubuntu/Debian: `.deb` para x64 o ARM64.
- Fedora y derivados: `.rpm` para x64 o ARM64.
- macOS: `.dmg` separado para Intel (`osx-x64`) y Apple Silicon (`osx-arm64`).

Todos incluyen .NET y el icono oficial. Lee **INSTALACION.md** y comprueba **SHA256SUMS.txt**. Los paquetes de Linux declaran sus bibliotecas del sistema; se requiere X11 o XWayland. Los datos locales y respaldos no están incluidos en los instaladores y se conservan al desinstalar.

### Validación y firma

Compilaciones y pruebas automatizadas en runners nativos de Windows, Linux x64/ARM64 y macOS Intel/Apple Silicon. Se verifican las arquitecturas, recursos, licencias, instalación/desinstalación aislada de Windows y DEB, contenido RPM e integridad/montaje de DMG. Las pruebas Avalonia utilizan Headless; no se afirma revisión manual de todas las pantallas en estos equipos.

**Sin firma oficial de Windows ni Apple Developer ID/notarización.** Windows/macOS pueden mostrar advertencias. La firma ad hoc de macOS valida el código nativo, pero no identifica al desarrollador. No se solicita desactivar globalmente la seguridad del sistema.

Licencia MIT. Repositorio: https://github.com/danielpose1996-stack/SIGNA
