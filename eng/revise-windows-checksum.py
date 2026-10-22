"""Revise only the Windows installer hash, preserving all update-package entries."""
import hashlib
import re
import sys
from pathlib import Path


def revise(original, installer, output, version):
    if not re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)', version):
        raise ValueError('Versión inválida')
    expected = f'SIGNA-{version}-windows-x64-setup.exe'
    if installer.name != expected or installer.stat().st_size <= 1_000_000:
        raise ValueError('Nombre o tamaño de instalador incorrecto')
    with installer.open('rb') as stream:
        if stream.read(2) != b'MZ':
            raise ValueError('El candidato no es un ejecutable Windows')
    lines = original.read_text(encoding='utf-8-sig').splitlines()
    seen = set()
    selected = None
    for index, line in enumerate(lines):
        match = re.fullmatch(r'([0-9a-fA-F]{64})  ([A-Za-z0-9_.-]+)', line)
        if not match or match[2] in seen or match[2].startswith('.'):
            raise ValueError('Manifiesto inválido, repetido o con rutas inseguras')
        seen.add(match[2])
        if match[2] == expected:
            selected = index
    if selected is None:
        raise ValueError('El manifiesto original no contiene el instalador solicitado')
    with installer.open('rb') as stream:
        digest = hashlib.file_digest(stream, 'sha256').hexdigest()
    lines[selected] = f'{digest}  {expected}'
    with output.open('x', encoding='utf-8', newline='\n') as stream:
        stream.write('\n'.join(lines) + '\n')
    print(f'Solo se actualizó el SHA-256 de {expected}; los demás registros se conservaron')


if __name__ == '__main__':
    revise(*(Path(p) for p in sys.argv[1:4]), sys.argv[4])
