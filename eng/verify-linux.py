import struct
import sys
from pathlib import Path

stage=Path(sys.argv[1]);rid=sys.argv[2];machine={'linux-x64':62,'linux-arm64':183}[rid]
root=Path(__file__).resolve().parent.parent
for name in ['GestorDocente.Desktop','libSkiaSharp.so','libHarfBuzzSharp.so','libe_sqlite3.so','libcoreclr.so']:
    file=stage/'usr/lib/signa'/name;data=file.read_bytes()
    assert data[:4]==b'\x7fELF' and struct.unpack_from('<H',data,18)[0]==machine,name
assert (stage/'usr/share/pixmaps/signa.png').read_bytes()==(root/'src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_512.png').read_bytes()
assert 'StartupWMClass=SIGNA' in (stage/'usr/share/applications/signa.desktop').read_text()
for file in stage.rglob('*'):
    if file.is_file():
        assert file.suffix.lower() not in ('.db','.sqlite','.sqlite3','.aulabk','.key','.pem','.pfx'),file
        assert 'AulaRegistro' not in file.parts,file
assert (stage/'usr/lib/signa/LICENSE').is_file()
assert (stage/'usr/lib/signa/Licenses/DEPENDENCIES.md').is_file()
print(f'Paquete {rid}: arquitectura, icono, licencia y exclusión de datos correctos')
