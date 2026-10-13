#!/bin/bash
set -euo pipefail
version=${1:-1.0.0};rid=${2:-osx-arm64}
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || exit 1
case "$rid" in osx-x64|osx-arm64) ;; *) exit 1;; esac
repo=$(cd -- "$(dirname -- "$0")/.." && pwd);cd "$repo"
base="$repo/artifacts/packages/macos/$rid"
publish="$base/publish";bundle="$base/SIGNA.app";stage="$base/dmg"
release="$repo/artifacts/packages/release"
[[ ! -e "$base" ]] || { echo 'Utiliza un checkout limpio para empaquetar' >&2; exit 1; }
mkdir -p "$release" "$stage"
dotnet publish src/GestorDocente.Desktop -c Release -r "$rid" --self-contained true -p:UseAppHost=true -p:PublishSingleFile=true "-p:Version=$version" -p:NuGetLockFilePath=obj/release.packages.lock.json -o "$publish"
python3 eng/copy-licenses.py "$publish" "$rid"
ditto "$publish/" "$bundle/Contents/MacOS/"
# Documentación y licencias son recursos, no código del bundle.
mv "$bundle/Contents/MacOS/Licenses" "$bundle/Contents/Resources/Licenses"
mv "$bundle/Contents/MacOS/LICENSE" "$bundle/Contents/Resources/LICENSE"
chmod +x "$bundle/Contents/MacOS/GestorDocente.Desktop"
# Firma local de integridad, no certificado Developer ID ni notarización.
# Los assemblies están en el ejecutable single-file. Sellar los componentes
# nativos antes del ejecutable/contenedor; ditto conserva los atributos.
find "$bundle/Contents/MacOS" -type f ! -name 'GestorDocente.Desktop' -print0 | while IFS= read -r -d '' code_file; do
  codesign --force --sign - "$code_file"
done
codesign --force --sign - --entitlements eng/macos.entitlements "$bundle/Contents/MacOS/GestorDocente.Desktop"
codesign --force --sign - --entitlements eng/macos.entitlements "$bundle"
codesign --verify --deep --strict "$bundle"
python3 eng/verify-macos.py "$bundle" "$rid" "$version"
ditto "$bundle" "$stage/SIGNA.app"
ln -s /Applications "$stage/Applications"
cp LICENSE "$stage/LICENCIA.txt"
cp eng/INSTALL-macos.txt "$stage/INSTALAR.txt"
image="$release/SIGNA-$version-$rid.dmg"
hdiutil create -volname "SIGNA $version" -srcfolder "$stage" -ov -format UDZO "$image"
hdiutil verify "$image"
mount="$base/mounted"
mkdir -p "$mount"
hdiutil attach "$image" -readonly -nobrowse -mountpoint "$mount"
trap 'hdiutil detach "$mount"' EXIT
test -x "$mount/SIGNA.app/Contents/MacOS/GestorDocente.Desktop"
test -L "$mount/Applications"
cmp src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_macos.icns "$mount/SIGNA.app/Contents/Resources/SIGNA_icono_macos.icns"
codesign --verify --deep --strict "$mount/SIGNA.app"
