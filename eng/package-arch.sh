#!/bin/bash
set -euo pipefail
version=${1:-1.5.0}
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || exit 1
repo=$(cd -- "$(dirname -- "$0")/.." && pwd)
stage="$repo/artifacts/packages/staging/linux-x64"
release="$repo/artifacts/packages/release"
[[ -x "$stage/usr/lib/signa/GestorDocente.Desktop" ]] || { echo 'Primero genera el payload Linux x64' >&2; exit 1; }
[[ ! -e "$release/SIGNA-$version-archlinux-x64.pkg.tar.zst" ]] || { echo 'El paquete Arch ya existe; no se sobrescribe' >&2; exit 1; }
# Official Arch Linux base-devel image, reviewed and pinned to its published manifest.
image='archlinux:base-devel@sha256:0ccc8b2e55c0c26a6e501e892cc6b1cdea7cfdf77e432941320148b4dabd2bff'
docker run --rm --platform linux/amd64 --env "SIGNA_VERSION=$version" \
  --mount "type=bind,source=$repo,target=/repo,readonly" \
  --mount "type=bind,source=$stage,target=/payload,readonly" \
  --mount "type=bind,source=$release,target=/release" \
  "$image" bash /repo/eng/archlinux/build-and-test.sh
