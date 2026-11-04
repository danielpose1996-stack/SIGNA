#!/bin/bash
# Solo CI: instalar/desinstalar en un contenedor Debian desechable, nunca en el host.
set -euo pipefail
[[ -f /.dockerenv && "${SIGNA_EPHEMERAL_TEST:-}" = 1 && "$EUID" = 0 ]] || {
  echo 'Esta prueba requiere un contenedor efímero autorizado; no ejecutarla en el equipo personal.' >&2
  exit 1
}
. /etc/os-release
[[ "$ID" = debian && "$VERSION_ID" = 13 ]] || { echo 'Se requiere Debian 13' >&2; exit 1; }
version=${1:?Indica la versión}
rid=${2:?Indica linux-x64 o linux-arm64}
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || exit 1
case "$rid" in linux-x64) arch=amd64;; linux-arm64) arch=arm64;; *) exit 1;; esac
[[ "$(dpkg --print-architecture)" = "$arch" ]] || { echo 'Arquitectura incorrecta' >&2; exit 1; }
repo=$(cd -- "$(dirname -- "$0")/.." && pwd)
mode=${3:-all}
case "$mode" in
  all)
    release="$repo/artifacts/packages/release"
    deb="$release/SIGNA-$version-$rid.deb"
    image="$release/SIGNA-$version-$rid.AppImage"
    test -x "$image";;
  debian-only)
    deb="$repo/artifacts/debian/release/SIGNA-$version-debian13-${rid#linux-}.deb";;
  *) exit 1;;
esac
test -f "$deb"
test "$(dpkg-deb -f "$deb" Architecture)" = "$arch"
export DEBIAN_FRONTEND=noninteractive
test_home=$(mktemp -d /tmp/signa-debian-XXXXXX)
printf 'respaldo ficticio anterior a la instalacion\n' > "$test_home/respaldo-previo.aulabk"
apt-get update
apt-get install -y --no-install-recommends "$deb" python3 xvfb xauth x11-utils fonts-dejavu-core libfuse2t64
grep -q 'respaldo ficticio anterior a la instalacion' "$test_home/respaldo-previo.aulabk"
# La resolución debe usar ICU de Debian, sin instalar el paquete de Ubuntu.
test "$(dpkg-query -W -f='${Status}' libicu76)" = 'install ok installed'
if dpkg-query -W -f='${Status}' libicu74 2>/dev/null | grep -qx 'install ok installed'; then
  echo 'La prueba no debe instalar ICU de Ubuntu en Debian' >&2
  exit 1
fi
signa --version | grep -F "SIGNA $version"
cmp "$repo/src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_512.png" /usr/share/pixmaps/signa.png
grep -q '^StartupWMClass=SIGNA$' /usr/share/applications/signa.desktop

check_libraries() {
  local directory=$1 file output
  for file in GestorDocente.Desktop libcoreclr.so libSkiaSharp.so libHarfBuzzSharp.so libe_sqlite3.so; do
    output=$(ldd "$directory/$file" 2>&1)
    if [[ "$output" = *'not found'* ]]; then
      printf 'Dependencia nativa no resuelta en %s:\n%s\n' "$file" "$output" >&2
      exit 1
    fi
  done
}
check_libraries /usr/lib/signa
# Un usuario y HOME nuevos evitan reutilizar el perfil o caché del runner.
chmod 755 "$test_home"
useradd --no-create-home --home-dir "$test_home" --shell /bin/sh signa-test
chown signa-test:signa-test "$test_home"
runuser -u signa-test -- env HOME="$test_home" xvfb-run -a python3 "$repo/eng/test-linux-desktop.py" /usr/bin/signa "$test_home/deb"

if [[ "$mode" = all ]]; then
# Validar también el contenido real de la AppImage, sin exigir /dev/fuse en Docker.
mkdir "$test_home/appimage"
(cd "$test_home/appimage"; "$image" --appimage-extract >/dev/null)
appdir="$test_home/appimage/squashfs-root"
check_libraries "$appdir/usr/bin"
if ! ldd "$appdir/usr/bin/UpdateNix" > "$test_home/updater-libraries.txt" 2>&1; then
  # Algunas ediciones del actualizador son ELF estáticos sin dependencias dinámicas.
  grep -Eq 'not a dynamic executable|statically linked' "$test_home/updater-libraries.txt" || {
    cat "$test_home/updater-libraries.txt"; exit 1;
  }
fi
if grep -q 'not found' "$test_home/updater-libraries.txt"; then cat "$test_home/updater-libraries.txt"; exit 1; fi
APPIMAGE="$image" "$appdir/AppRun" --version | grep -F "SIGNA $version"
runuser -u signa-test -- env HOME="$test_home" APPIMAGE="$image" xvfb-run -a python3 "$repo/eng/test-linux-desktop.py" "$appdir/AppRun" "$test_home/appimage-profile"
fi

# Desinstalar el paquete conserva los datos SQLite y el respaldo ficticios del usuario.
sha256sum "$test_home/deb/gestor-docente-demo.db" "$test_home/deb/respaldo-ficticio.aulabk" "$test_home/respaldo-previo.aulabk" > "$test_home/before-removal.sha256"
apt-get remove -y signa
test ! -e /usr/bin/signa
sha256sum --check "$test_home/before-removal.sha256"
echo "Debian 13 $arch ($mode): apt, ELF, icono, arranque X11/SQLite y conservación de datos correctos."
