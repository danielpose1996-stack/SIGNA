Primera distribución instalable de **SIGNA — Sistema Integral de Gestión de Notas y Asistencias**.

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
