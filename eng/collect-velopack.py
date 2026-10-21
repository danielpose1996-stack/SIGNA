"""Validate a platform's update feed and copy only its full package and index."""
import hashlib
import json
import shutil
import sys
import zipfile
from pathlib import Path, PurePosixPath
import xml.etree.ElementTree as ET


def validate(folder, channel, version):
    feed_path = folder / f'releases.{channel}.json'
    feed = json.loads(feed_path.read_text(encoding='utf-8-sig'))
    assets = feed.get('Assets', [])
    if len(assets) != 1:
        raise ValueError('El canal debe contener exactamente un paquete completo')
    asset = assets[0]
    name = asset.get('FileName', '')
    if asset.get('PackageId') != 'SIGNA' or asset.get('Version') != version or asset.get('Type') != 'Full':
        raise ValueError('Identidad, versión o tipo de actualización incorrectos')
    if not name or Path(name).name != name or '/' in name or '\\' in name or not name.endswith('-full.nupkg'):
        raise ValueError('Nombre de paquete inseguro')
    package = folder / name
    data = package.read_bytes()
    if len(data) != asset.get('Size') or hashlib.sha256(data).hexdigest().upper() != asset.get('SHA256', '').upper():
        raise ValueError('El paquete no coincide con el tamaño/SHA-256 del feed')
    with zipfile.ZipFile(package) as archive:
        manifests = []
        for info in archive.infolist():
            path = PurePosixPath(info.filename)
            if path.is_absolute() or '..' in path.parts or '\\' in info.filename or ':' in info.filename:
                raise ValueError('Ruta insegura en el paquete')
            if path.suffix.lower() in ('.db', '.sqlite', '.sqlite3', '.aulabk', '.key', '.pem', '.pfx'):
                raise ValueError('El paquete contiene datos o secretos excluidos')
            if path.suffix == '.nuspec':
                manifests.append(ET.fromstring(archive.read(info)))
        if len(manifests) != 1:
            raise ValueError('Manifiesto de aplicación ausente o ambiguo')
        fields = {node.tag.rsplit('}', 1)[-1]: node.text for node in manifests[0].iter()}
        os, architecture = channel.split('-')
        if (fields.get('id') != 'SIGNA' or fields.get('version') != version or
                fields.get('channel') != channel or fields.get('rid') != channel or
                fields.get('os') != os or fields.get('machineArchitecture') != architecture):
            raise ValueError('La identidad, versión, canal o arquitectura interna del paquete no coincide')
    return feed_path, package


def main():
    folder, destination = Path(sys.argv[1]), Path(sys.argv[2])
    channel, version = sys.argv[3:5]
    files = validate(folder, channel, version)
    destination.mkdir(parents=True, exist_ok=True)
    if any((destination / file.name).exists() for file in files):
        raise ValueError('No se sobrescriben paquetes o feeds ya publicados')
    for file in files:
        shutil.copyfile(file, destination / file.name)
    print(f'{channel}: identidad, versión, rutas, privacidad y SHA-256 verificados')


if __name__ == '__main__':
    main()
