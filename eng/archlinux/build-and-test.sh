#!/bin/bash
set -euo pipefail
version=${SIGNA_VERSION:?Falta SIGNA_VERSION}
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || exit 1
[[ $(uname -m) == x86_64 && -f /etc/arch-release ]] || exit 1

# This script runs only in an ephemeral official Arch container, never on the user's system.
pacman -Syu --noconfirm --needed archlinux-keyring python gcc-libs zlib openssl icu krb5 \
  libx11 libice libsm fontconfig freetype2 ca-certificates tzdata hicolor-icon-theme xorg-xwayland
python /repo/eng/verify-linux.py /payload linux-x64

build=/tmp/signa-arch-build
[[ ! -e "$build" ]] || exit 1
useradd --create-home builder
install -d -o builder -g builder "$build"
tar -C /payload -czf "$build/signa-payload.tar.gz" usr
checksum=$(sha256sum "$build/signa-payload.tar.gz" | cut -d' ' -f1)
sed -e "s/__VERSION__/$version/g" -e "s/__SHA256__/$checksum/g" /repo/eng/archlinux/PKGBUILD > "$build/PKGBUILD"
chown builder:builder "$build/PKGBUILD" "$build/signa-payload.tar.gz"
cd "$build"
runuser -u builder -- makepkg --noconfirm
package="$build/signa-$version-1-x86_64.pkg.tar.zst"
test -f "$package"
test "$(pacman -Qp "$package")" = "signa $version-1"
pacman -Qip "$package"
bsdtar -xOf "$package" .PKGINFO > "$build/metadata.txt"
grep -Fx 'arch = x86_64' "$build/metadata.txt"
grep -Fx 'depend = xorg-xwayland' "$build/metadata.txt"

# Install/remove inside the container. --version exits before UI or academic storage is opened.
data=/home/builder/.local/share/AulaRegistro
install -d "$data"
printf 'datos ficticios conservados\n' > "$data/prueba-empaquetado.txt"
pacman -U --noconfirm "$package"
pacman -Qk signa
test -x /usr/bin/signa && test -x /usr/lib/signa/GestorDocente.Desktop
/usr/bin/signa --version | grep -Fx "SIGNA $version"
cmp /payload/usr/share/pixmaps/signa.png /usr/share/pixmaps/signa.png
test -f /usr/share/licenses/signa/LICENSE
test -f /usr/share/applications/signa.desktop
pacman -R --noconfirm signa
grep -Fx 'datos ficticios conservados' "$data/prueba-empaquetado.txt"
test ! -e /usr/bin/signa
cp "$package" "/release/SIGNA-$version-archlinux-x64.pkg.tar.zst"
printf 'Arch Linux: paquete, dependencias, versión, icono e instalación/desinstalación verificados\n'
