#!/bin/sh
# Registra únicamente el lanzador y el icono del usuario; no abre SIGNA.
set -eu

if [ "$(uname -s)" != Linux ]; then
    printf '%s\n' 'Este registro del icono corresponde a Linux.' >&2
    exit 1
fi

signa_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)
signa_executable="$signa_directory/GestorDocente.Desktop"
signa_icon="$signa_directory/SIGNA_icono_512.png"
if [ ! -f "$signa_executable" ] || [ ! -f "$signa_icon" ]; then
    printf '%s\n' 'Conserva este script junto al ejecutable y el icono de la carpeta publish.' >&2
    exit 1
fi
if printf '%s' "$signa_directory" | LC_ALL=C grep -q '[[:cntrl:]]'; then
    printf '%s\n' 'La ruta de SIGNA contiene caracteres de control; mueve la carpeta antes de registrar el acceso.' >&2
    exit 1
fi

signa_data_directory=${XDG_DATA_HOME:-"$HOME/.local/share"}
case "$signa_data_directory" in
    /*) ;;
    *) printf '%s\n' 'XDG_DATA_HOME debe ser una ruta absoluta.' >&2; exit 1 ;;
esac
signa_applications="$signa_data_directory/applications"
signa_icons="$signa_data_directory/icons/hicolor/512x512/apps"

# Exec se decodifica primero como Desktop Entry y luego como argumento entre
# comillas. Escapar ambos niveles, incluidos los códigos de campo (%).
signa_exec_value=$(printf '%s' "$signa_executable" | sed \
    -e 's/\\/\\\\\\\\/g' -e 's/"/\\\\"/g' -e 's/`/\\\\`/g' -e 's/\$/\\\\$/g' -e 's/%/%%/g')

mkdir -p -- "$signa_applications" "$signa_icons"
chmod +x -- "$signa_executable"
cp -- "$signa_icon" "$signa_icons/signa.png"
cat > "$signa_applications/signa.desktop" <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=SIGNA
GenericName=Sistema Integral de Gestión de Notas y Asistencias
Comment=Gestión local de materias, notas y asistencias
Exec="$signa_exec_value"
Icon=signa
Terminal=false
Categories=Education;
StartupWMClass=SIGNA
EOF
chmod 644 -- "$signa_applications/signa.desktop" "$signa_icons/signa.png"
printf '%s\n' 'Acceso SIGNA registrado para este usuario con el icono oficial.'
