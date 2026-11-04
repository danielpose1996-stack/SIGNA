"""Adaptar un DEB publicado a Debian 13 conservando todos sus binarios y versión."""
import argparse
import hashlib
from pathlib import Path
import re
import stat


def digest(file):
    with file.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def verify_source(source, manifest):
    matches = []
    for line in manifest.read_text(encoding='utf-8-sig').splitlines():
        checksum, name = line.split('  ', 1)
        if name == source.name:
            if not re.fullmatch('[a-f0-9]{64}', checksum):
                raise ValueError('SHA-256 original inválido')
            matches.append(checksum)
    if len(matches) != 1 or digest(source) != matches[0]:
        raise ValueError('El DEB original no coincide con el SHA-256 publicado')
    return matches[0]


def debian_control(text, version, architecture):
    if not re.fullmatch(r'\d+\.\d+\.\d+', version) or architecture not in ('amd64', 'arm64'):
        raise ValueError('Versión o arquitectura inválida')
    fields = {}
    for line in text.splitlines():
        if line and not line[0].isspace():
            key, value = line.split(':', 1)
            if key in fields:
                raise ValueError('Campo duplicado en control')
            fields[key] = value.strip()
    if (fields.get('Package'), fields.get('Version'), fields.get('Architecture')) != ('signa', version, architecture):
        raise ValueError('La identidad, versión o arquitectura del DEB original no corresponde')
    depends = fields.get('Depends', '')
    if [part.strip() for part in depends.split(',')].count('libicu74') != 1:
        raise ValueError('El paquete original no contiene la dependencia ICU esperada')
    adjusted = re.sub(r'(?<![a-z0-9-])libicu74(?![a-z0-9-])', 'libicu76', depends)
    return re.sub(r'^Depends: .*$', lambda _: 'Depends: ' + adjusted, text, count=1, flags=re.MULTILINE)


def payload_manifest(root):
    entries = {}
    for path in root.rglob('*'):
        mode = path.lstat().st_mode
        name = path.relative_to(root).as_posix()
        if path.is_symlink():
            content = ('symlink', str(path.readlink()))
        elif path.is_file():
            content = ('file', digest(path))
        elif path.is_dir():
            content = ('directory', '')
        else:
            raise ValueError('Contenido inesperado en el DEB')
        entries[name] = (stat.S_IMODE(mode), *content)
    return entries


def compare_payload(first, second):
    if payload_manifest(first) != payload_manifest(second):
        raise ValueError('El reempaquetado alteró archivos, binarios, enlaces o permisos del programa')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    verify = commands.add_parser('verify-source')
    verify.add_argument('source', type=Path); verify.add_argument('manifest', type=Path)
    control = commands.add_parser('rewrite-control')
    control.add_argument('file', type=Path); control.add_argument('version'); control.add_argument('architecture')
    compare = commands.add_parser('compare-payload')
    compare.add_argument('first', type=Path); compare.add_argument('second', type=Path)
    args = parser.parse_args()
    if args.command == 'verify-source':
        print(verify_source(args.source, args.manifest))
    elif args.command == 'rewrite-control':
        updated = debian_control(args.file.read_text(encoding='utf-8'), args.version, args.architecture)
        args.file.write_text(updated, encoding='utf-8', newline='\n')
    else:
        compare_payload(args.first, args.second)
        print('Binarios, recursos, enlaces y permisos del programa idénticos al DEB original')


if __name__ == '__main__':
    main()
