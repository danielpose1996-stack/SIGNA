import hashlib
import importlib.util
import sys
from pathlib import Path

folder=Path(sys.argv[1]);version=sys.argv[2]
expected=[f'SIGNA-{version}-windows-x64-setup.exe']
expected += [f'SIGNA-{version}-{rid}.deb' for rid in ('linux-x64','linux-arm64')]
expected += [f'SIGNA-{version}-{rid}.dmg' for rid in ('osx-x64','osx-arm64')]
expected += [f'SIGNA-{version}-{rid}.AppImage' for rid in ('linux-x64','linux-arm64')]
# Cada canal tiene un feed exclusivo y un paquete completo con identidad/hash verificados.
module=importlib.util.spec_from_file_location('velopack_artifacts',Path(__file__).with_name('collect-velopack.py'))
collector=importlib.util.module_from_spec(module);module.loader.exec_module(collector)
feeds=[]
for channel in ('win-x64','linux-x64','linux-arm64','osx-x64','osx-arm64'):
    feed,package=collector.validate(folder,channel,version)
    expected += [feed.name,package.name];feeds.append(feed.name)
if sorted(p.name for p in folder.iterdir() if p.is_file())!=sorted(expected):
    raise ValueError('Conjunto incompleto o inesperado de instaladores/actualizaciones')
lines=[]
for name in sorted(expected):
    file=folder/name
    if name not in feeds and file.stat().st_size<=1_000_000:
        raise ValueError(f'Archivo demasiado pequeño: {name}')
    lines.append(f'{hashlib.sha256(file.read_bytes()).hexdigest()}  {name}')
(folder/'SHA256SUMS.txt').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Siete distribuciones Windows/macOS/Ubuntu y cinco canales de actualización verificados; SHA-256 generados')
