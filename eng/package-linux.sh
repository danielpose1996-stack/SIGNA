#!/bin/bash
set -euo pipefail
version=${1:-1.6.1}
rid=${2:-linux-x64}
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo 'Versión inválida' >&2; exit 1; }
case "$rid" in linux-x64) deb_arch=amd64;; linux-arm64) deb_arch=arm64;; *) exit 1;; esac
repo=$(cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repo"
# Las nuevas distribuciones Linux se construyen y verifican en Ubuntu.
. /etc/os-release
[[ "$ID" = ubuntu && "$VERSION_ID" = 24.04 ]] || { echo 'Empaqueta en Ubuntu 24.04 para mantener la plataforma verificada' >&2; exit 1; }
publish="$repo/artifacts/packages/publish/$rid"
stage="$repo/artifacts/packages/staging/$rid"
release="$repo/artifacts/packages/release"
[[ ! -e "$publish" && ! -e "$stage" ]] || { echo 'Utiliza un checkout limpio para empaquetar' >&2; exit 1; }
mkdir -p "$release" "$stage/usr/lib/signa" "$stage/usr/bin" "$stage/usr/share/applications" "$stage/usr/share/icons/hicolor/512x512/apps" "$stage/usr/share/pixmaps" "$stage/usr/share/doc/signa"
dotnet publish src/GestorDocente.Desktop -c Release -r "$rid" --self-contained true -p:UseAppHost=true "-p:Version=$version" -p:NuGetLockFilePath=obj/release.packages.lock.json -o "$publish"
python3 eng/copy-licenses.py "$publish" "$rid"
# AppImage actualizable; los paquetes nativos siguientes siguen bajo su gestor.
velopack="$repo/artifacts/packages/velopack/$rid"
dotnet tool restore
dotnet tool run vpk -- --skip-updates --yes pack --packId SIGNA --packVersion "$version" --packDir "$publish" --mainExe GestorDocente.Desktop --runtime "$rid" --channel "$rid" --packTitle SIGNA --packAuthors SIGNA --icon src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_512.png --categories Education --outputDir "$velopack"
python3 eng/collect-velopack.py "$velopack" "$release" "$rid" "$version"
shopt -s nullglob
images=("$velopack/"*.AppImage)
[[ ${#images[@]} -eq 1 ]] || { echo 'Se esperaba una única AppImage' >&2; exit 1; }
cp "${images[0]}" "$release/SIGNA-$version-$rid.AppImage"
chmod +x "$release/SIGNA-$version-$rid.AppImage"
# Extraer solo en el runner efímero evita exigir FUSE o un escritorio activo.
mkdir -p "$velopack/probe"
(cd "$velopack/probe"; "$release/SIGNA-$version-$rid.AppImage" --appimage-extract >/dev/null)
appdir="$velopack/probe/squashfs-root"
test -x "$appdir/usr/bin/UpdateNix"
test -f "$appdir/usr/bin/sq.version"
APPIMAGE="$release/SIGNA-$version-$rid.AppImage" "$appdir/AppRun" --version | grep -F "SIGNA $version"
cp -a "$publish/." "$stage/usr/lib/signa/"
find "$stage" -type d -exec chmod 755 {} +
find "$stage" -type f -exec chmod 644 {} +
chmod 755 "$stage/usr/lib/signa/GestorDocente.Desktop" "$stage/usr/lib/signa/Registrar-icono.sh"
printf '#!/bin/sh\nexec /usr/lib/signa/GestorDocente.Desktop "$@"\n' > "$stage/usr/bin/signa"
chmod 755 "$stage/usr/bin/signa"
cp src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_512.png "$stage/usr/share/icons/hicolor/512x512/apps/signa.png"
cp src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_512.png "$stage/usr/share/pixmaps/signa.png"
cp LICENSE "$stage/usr/share/doc/signa/copyright"
cat > "$stage/usr/share/applications/signa.desktop" <<'EOF'
[Desktop Entry]
Version=1.0
Type=Application
Name=SIGNA
GenericName=Sistema Integral de Gestión de Notas y Asistencias
Comment=Gestión local de materias, notas y asistencias
Exec=/usr/bin/signa
Icon=signa
Terminal=false
Categories=Education;
StartupWMClass=SIGNA
EOF
mkdir -p "$stage/DEBIAN"
cat > "$stage/DEBIAN/control" <<EOF
Package: signa
Version: $version
Section: education
Priority: optional
Architecture: $deb_arch
Maintainer: SIGNA <danielpose1996-stack@users.noreply.github.com>
Homepage: https://github.com/danielpose1996-stack/SIGNA
Depends: libc6 (>= 2.39), libgcc-s1, libstdc++6, zlib1g, libssl3t64, libicu74, libgssapi-krb5-2, libx11-6, libice6, libsm6, libfontconfig1, libfreetype6, ca-certificates, tzdata
Installed-Size: $(du -sk "$stage/usr" | cut -f1)
Description: Sistema Integral de Gestión de Notas y Asistencias
 Aplicación de escritorio local para docentes, con notas, asistencias,
 reportes PDF y respaldos cifrados. Incluye el runtime de .NET.
EOF
dpkg-deb --root-owner-group --build "$stage" "$release/SIGNA-$version-$rid.deb"
python3 eng/verify-linux.py "$stage" "$rid"
# Instalación aislada en el runner: ni UI ni directorios del docente.
data="${XDG_DATA_HOME:-$HOME/.local/share}/AulaRegistro"
mkdir -p "$data"
sentinel="$data/prueba-empaquetado.txt"
[[ ! -e "$sentinel" ]] || exit 1
printf 'datos ficticios de prueba\n' > "$sentinel"
sudo apt-get install -y "$release/SIGNA-$version-$rid.deb"
test -x /usr/bin/signa && test -x /usr/lib/signa/GestorDocente.Desktop
/usr/bin/signa --version | grep -F "SIGNA $version"
cmp src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_512.png /usr/share/pixmaps/signa.png
sudo apt-get remove -y signa
grep -q 'datos ficticios de prueba' "$sentinel"
