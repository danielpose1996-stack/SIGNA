import hashlib
import sys
from pathlib import Path

folder=Path(sys.argv[1]);version=sys.argv[2]
expected=[f'SIGNA-{version}-windows-x64-setup.exe']
expected += [f'SIGNA-{version}-{rid}.{ext}' for rid in ('linux-x64','linux-arm64') for ext in ('deb','rpm')]
expected += [f'SIGNA-{version}-{rid}.dmg' for rid in ('osx-x64','osx-arm64')]
expected += [f'SIGNA-{version}-archlinux-x64.pkg.tar.zst']
assert sorted(p.name for p in folder.iterdir() if p.is_file())==sorted(expected),'Conjunto incompleto o inesperado de instaladores'
lines=[]
for name in sorted(expected):
    file=folder/name;assert file.stat().st_size>1_000_000,name
    lines.append(f'{hashlib.sha256(file.read_bytes()).hexdigest()}  {name}')
(folder/'SHA256SUMS.txt').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Ocho instaladores completos; SHA-256 generados')
