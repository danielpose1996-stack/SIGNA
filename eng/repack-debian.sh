#!/bin/bash
set -euo pipefail
version=${1:?Indica la versión ya publicada}
rid=${2:?Indica linux-x64 o linux-arm64}
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || exit 1
case "$rid" in linux-x64) arch=amd64; label=x64;; linux-arm64) arch=arm64; label=arm64;; *) exit 1;; esac
[[ "$(dpkg --print-architecture)" = "$arch" ]] || exit 1
repo=$(cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repo"
input="$repo/artifacts/debian/source"
stage="$repo/artifacts/debian/staging/$rid"
verify="$repo/artifacts/debian/verify/$rid"
release="$repo/artifacts/debian/release"
source="$input/SIGNA-$version-$rid.deb"
[[ ! -e "$stage" && ! -e "$verify" ]] || { echo 'Utiliza un checkout limpio' >&2; exit 1; }
python3 eng/debian-package.py verify-source "$source" "$input/SHA256SUMS.txt"
mkdir -p "$stage" "$verify/original" "$verify/debian"
dpkg-deb --raw-extract "$source" "$stage"
# Snapshot antes de cambiar únicamente el metadato ICU.
cp "$stage/DEBIAN/control" "$input/control-original-$rid.txt"
python3 eng/debian-package.py rewrite-control "$stage/DEBIAN/control" "$version" "$arch"
mkdir -p "$release"
target="$release/SIGNA-$version-debian13-$label.deb"
test ! -e "$target"
dpkg-deb --root-owner-group --build "$stage" "$target"
dpkg-deb --extract "$source" "$verify/original"
dpkg-deb --extract "$target" "$verify/debian"
python3 eng/debian-package.py compare-payload "$verify/original" "$verify/debian"
python3 eng/verify-linux.py "$stage" "$rid"
test "$(dpkg-deb -f "$target" Version)" = "$version"
test "$(dpkg-deb -f "$target" Architecture)" = "$arch"
