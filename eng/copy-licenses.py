"""Incluye licencias/avisos de los paquetes resueltos, sin descargar dependencias."""
import argparse
import json
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path

p=argparse.ArgumentParser();p.add_argument('publish',type=Path);p.add_argument('rid');a=p.parse_args()
root=Path(__file__).resolve().parent.parent
assets=json.loads((root/'src/GestorDocente.Desktop/obj/project.assets.json').read_text(encoding='utf-8-sig'))
folders=[Path(x) for x in assets['packageFolders']]
destination=a.publish/'Licenses';destination.mkdir(exist_ok=True)
shutil.copy2(root/'LICENSE',a.publish/'LICENSE')
notices=['# Dependencias distribuidas con SIGNA','', 'SIGNA: licencia MIT. Las bibliotecas conservan sus licencias originales.','']
packages={k:v['path'] for k,v in assets['libraries'].items() if v.get('type')=='package'}
runtime_file=a.publish/'GestorDocente.Desktop.runtimeconfig.json'
if not runtime_file.exists():
    # En single-file el JSON se integra en el apphost; el generado por build
    # sigue disponible y contiene la versión exacta utilizada por el SDK.
    runtime_file=root/'src/GestorDocente.Desktop/bin/Release/net10.0'/a.rid/'GestorDocente.Desktop.runtimeconfig.json'
runtime=json.loads(runtime_file.read_text())['runtimeOptions']
version=next(f['version'] for f in runtime['includedFrameworks'] if f['name']=='Microsoft.NETCore.App')
packages[f'Microsoft.NETCore.App.Runtime.{a.rid}/{version}']=f'microsoft.netcore.app.runtime.{a.rid}/{version}'
for name,relative in sorted(packages.items()):
    package=next((folder/relative for folder in folders if (folder/relative).is_dir()),None)
    if package is None:raise RuntimeError(f'No se encontró el paquete resuelto {name}')
    target=destination/name.replace('/','-');target.mkdir(exist_ok=True)
    for file in package.iterdir():
        if file.is_file() and ('license' in file.name.lower() or 'notice' in file.name.lower()) and file.suffix.lower() in ('.txt','.md',''):
            shutil.copy2(file,target/file.name)
    spec=next(package.glob('*.nuspec'));metadata=ET.parse(spec).getroot()
    def value(key):
        return next((e.text or '' for e in metadata.iter() if e.tag.split('}')[-1]==key),'')
    notices.extend([f'## {name}',f"Licencia: {value('license') or value('licenseUrl') or 'Ver proyecto original'}",f"Proyecto: {value('projectUrl')}",f"Autores: {value('authors')}",''])
(destination/'DEPENDENCIES.md').write_text('\n'.join(notices)+'\n',encoding='utf-8')
print(f'Licencias y avisos incluidos: {len(packages)} paquetes resueltos')
