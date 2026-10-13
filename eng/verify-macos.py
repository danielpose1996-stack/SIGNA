import plistlib
import struct
import sys
from pathlib import Path

bundle=Path(sys.argv[1]);rid=sys.argv[2];version=sys.argv[3]
root=Path(__file__).resolve().parent.parent
info=plistlib.loads((bundle/'Contents/Info.plist').read_bytes())
assert info['CFBundleName']=='SIGNA' and info['CFBundleVersion']==version
assert info['CFBundleExecutable']=='GestorDocente.Desktop'
assert (bundle/'Contents/Resources'/info['CFBundleIconFile']).read_bytes()==(root/'src/GestorDocente.Desktop/Assets/Branding/SIGNA_icono_macos.icns').read_bytes()
machine={'osx-x64':0x01000007,'osx-arm64':0x0100000c}[rid]
for name in ['GestorDocente.Desktop','libSkiaSharp.dylib','libHarfBuzzSharp.dylib','libe_sqlite3.dylib','libcoreclr.dylib']:
    data=(bundle/'Contents/MacOS'/name).read_bytes()
    magic=struct.unpack_from('<I',data,0)[0]
    if magic==0xfeedfacf:assert struct.unpack_from('<I',data,4)[0]==machine,name
    else:
        # Las bibliotecas Avalonia pueden conservar un binario universal.
        assert data[:4] in (b'\xca\xfe\xba\xbe',b'\xca\xfe\xba\xbf'),name
        size=20 if data[:4]==b'\xca\xfe\xba\xbe' else 32
        assert machine in [struct.unpack_from('>I',data,8+i*size)[0] for i in range(struct.unpack_from('>I',data,4)[0])],name
assert (bundle/'Contents/MacOS/LICENSE').is_file()
for file in bundle.rglob('*'):
    assert file.suffix.lower() not in ('.db','.sqlite','.sqlite3','.aulabk','.key','.pem','.pfx'),file
print(f'{rid}: bundle, arquitectura, icono y licencias correctos')
