"""Agregar los DEB verificados a una Release existente sin reemplazar archivos."""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys


def main():
    folder = Path(sys.argv[1])
    version = sys.argv[2]
    if not re.fullmatch(r'\d+\.\d+\.\d+', version):
        raise ValueError('Versión inválida')
    repo = os.environ['GH_REPO']
    tag = 'v' + version

    def api():
        return json.loads(subprocess.check_output(['gh', 'api', f'repos/{repo}/releases/tags/{tag}'], encoding='utf-8'))

    before = api()
    if before['draft'] or before['prerelease'] or before.get('immutable'):
        raise ValueError('Se requiere una Release estable existente que admita adjuntos')
    names = [f'SIGNA-{version}-debian13-{arch}.deb' for arch in ('x64', 'arm64')]
    if sorted(path.name for path in folder.iterdir()) != sorted(names):
        raise ValueError('Se requieren exactamente los dos instaladores Debian verificados')
    hashes = {}
    for name in names:
        path = folder / name
        if path.stat().st_size < 1_000_000:
            raise ValueError('Instalador incompleto')
        with path.open('rb') as stream:
            hashes[name] = hashlib.file_digest(stream, 'sha256').hexdigest()
    manifest = folder / 'SHA256SUMS-debian13.txt'
    manifest.write_text(''.join(f'{hashes[name]}  {name}\n' for name in names), encoding='utf-8', newline='\n')
    hashes[manifest.name] = hashlib.sha256(manifest.read_bytes()).hexdigest()
    original = {a['name']: a for a in before['assets'] if a['name'] not in hashes}
    if len(original) != 19:
        raise ValueError('La Release no conserva el conjunto original esperado de 19 archivos')
    assets = {a['name']: a for a in before['assets']}
    for name, checksum in hashes.items():
        if name in assets:
            if assets[name].get('digest') != 'sha256:' + checksum:
                raise ValueError('Ya existe un adjunto diferente: ' + name)
        else:
            subprocess.run(['gh', 'release', 'upload', tag, str(folder / name), '--repo', repo], check=True)

    after = api()
    assets = {a['name']: a for a in after['assets']}
    if set(assets) != set(original) | set(hashes):
        raise ValueError('Conjunto inesperado de archivos tras publicar')
    for name, asset in original.items():
        if any(asset[key] != assets[name][key] for key in ('id', 'size', 'digest', 'updated_at')):
            raise ValueError('Se alteró un archivo original: ' + name)
    for name, checksum in hashes.items():
        if assets[name].get('digest') != 'sha256:' + checksum or assets[name]['state'] != 'uploaded':
            raise ValueError('El hash publicado no coincide: ' + name)
    if after['target_commitish'] != before['target_commitish'] or after['tag_name'] != tag:
        raise ValueError('Cambió el tag o commit objetivo')

    heading = f'### Instaladores adicionales Debian 13 — misma versión {version}'
    notes = Path('eng/RELEASE.md').read_text(encoding='utf-8')
    if heading not in notes:
        raise ValueError('Faltan las instrucciones de Debian de esta versión')
    if heading not in after['body']:
        body = folder.parent / 'notas-debian.md'
        body.write_text(after['body'].rstrip() + '\n\n' + heading + notes.split(heading, 1)[1], encoding='utf-8')
        subprocess.run(['gh', 'release', 'edit', tag, '--repo', repo, '--notes-file', str(body)], check=True)
    result = {'release': tag, 'url': after['html_url'], 'original_assets_unchanged': len(original),
              'added': hashes, 'asset_count': len(assets), 'target': after['target_commitish']}
    (folder.parent / 'publicacion.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
