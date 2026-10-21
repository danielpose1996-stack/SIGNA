#!/bin/bash
set -euo pipefail
version=${1:-1.5.0}
rid=${2:-linux-x64}
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo 'Versión inválida' >&2; exit 1; }
case "$rid" in linux-x64) deb_arch=amd64; rpm_arch=x86_64;; linux-arm64) deb_arch=arm64; rpm_arch=aarch64;; *) exit 1;; esac
repo=$(cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repo"
publish="$repo/artifacts/packages/publish/$rid"
stage="$repo/artifacts/packages/staging/$rid"
release="$repo/artifacts/packages/release"
[[ ! -e "$publish" && ! -e "$stage" ]] || { echo 'Utiliza un checkout limpio para empaquetar' >&2; exit 1; }
mkdir -p "$release" "$stage/usr/lib/signa" "$stage/usr/bin" "$stage/usr/share/applications" "$stage/usr/share/icons/hicolor/512x512/apps" "$stage/usr/share/pixmaps" "$stage/usr/share/doc/signa"
dotnet publish src/GestorDocente.Desktop -c Release -r "$rid" --self-contained true -p:UseAppHost=true "-p:Version=$version" -p:NuGetLockFilePath=obj/release.packages.lock.json -o "$publish"
python3 eng/copy-licenses.py "$publish" "$rid"
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
Depends: libc6 (>= 2.35), libgcc-s1, libstdc++6, zlib1g, libssl3 | libssl3t64, libicu74 | libicu72 | libicu70 | libicu76 | libicu77 | libicu78, libgssapi-krb5-2, libx11-6, libice6, libsm6, libfontconfig1, libfreetype6, ca-certificates, tzdata
Installed-Size: $(du -sk "$stage/usr" | cut -f1)
Description: Sistema Integral de Gestión de Notas y Asistencias
 Aplicación de escritorio local para docentes, con notas, asistencias,
 reportes PDF y respaldos cifrados. Incluye el runtime de .NET.
EOF
dpkg-deb --root-owner-group --build "$stage" "$release/SIGNA-$version-$rid.deb"
top="$repo/artifacts/packages/rpm/$rid"
mkdir -p "$top"/{BUILD,BUILDROOT,RPMS,SOURCES,SPECS,SRPMS}
# Tar de solo /usr: DEBIAN contiene metadatos exclusivos del otro formato.
tar -C "$stage" -czf "$top/SOURCES/signa.tar.gz" usr
cat > "$top/SPECS/signa.spec" <<EOF
Name: signa
Version: $version
Release: 1
Summary: Sistema Integral de Gestión de Notas y Asistencias
License: MIT
URL: https://github.com/danielpose1996-stack/SIGNA
Source0: signa.tar.gz
BuildArch: $rpm_arch
Requires: glibc >= 2.35, libgcc, libstdc++, zlib, openssl-libs, libicu, krb5-libs, libX11, libICE, libSM, fontconfig, freetype, ca-certificates, tzdata
%global debug_package %{nil}
%global _build_id_links none
%global __os_install_post %{nil}
%description
Aplicación de escritorio local para materias, estudiantes, notas,
asistencias, reportes PDF y respaldos cifrados. Incluye .NET.
%prep
%build
%install
mkdir -p %{buildroot}
tar -xzf %{SOURCE0} -C %{buildroot}
%files
%defattr(-,root,root,-)
/usr/bin/signa
/usr/lib/signa
/usr/share/applications/signa.desktop
/usr/share/icons/hicolor/512x512/apps/signa.png
/usr/share/pixmaps/signa.png
%doc /usr/share/doc/signa
EOF
rpmbuild --define "_topdir $top" --target "$rpm_arch" -bb "$top/SPECS/signa.spec"
cp "$top/RPMS/$rpm_arch/signa-$version-1.$rpm_arch.rpm" "$release/SIGNA-$version-$rid.rpm"
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
rpm -qpl "$release/SIGNA-$version-$rid.rpm" > "$repo/artifacts/packages/rpm-$rid-files.txt"
rpm -qp --requires "$release/SIGNA-$version-$rid.rpm"
