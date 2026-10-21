import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path


class ReleaseManifestTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.folder = Path(self.temporary.name)
        self.names = [
            'SIGNA-1.6.0-windows-x64-setup.exe',
            'SIGNA-1.6.0-linux-x64.deb', 'SIGNA-1.6.0-linux-x64.rpm',
            'SIGNA-1.6.0-linux-arm64.deb', 'SIGNA-1.6.0-linux-arm64.rpm',
            'SIGNA-1.6.0-osx-x64.dmg', 'SIGNA-1.6.0-osx-arm64.dmg',
            'SIGNA-1.6.0-archlinux-x64.pkg.tar.zst',
            'SIGNA-1.6.0-linux-x64.AppImage', 'SIGNA-1.6.0-linux-arm64.AppImage',
        ]
        for name in self.names:
            (self.folder / name).write_bytes(b'fixture-installer\n' * 60000)
        for channel in ('win-x64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64'):
            package = f'SIGNA-1.6.0-{channel}-full.nupkg'
            os, arch = channel.split('-')
            with zipfile.ZipFile(self.folder / package, 'w') as archive:
                archive.writestr('SIGNA.nuspec', f'<package><metadata><id>SIGNA</id><version>1.6.0</version><channel>{channel}</channel><rid>{channel}</rid><os>{os}</os><machineArchitecture>{arch}</machineArchitecture></metadata></package>')
                archive.writestr('lib/app/program', b'fixture-binary\n' * 80000)
            data = (self.folder / package).read_bytes()
            feed = f'releases.{channel}.json'
            (self.folder / feed).write_text(json.dumps({'Assets': [dict(PackageId='SIGNA', Version='1.6.0', Type='Full', FileName=package, SHA256=hashlib.sha256(data).hexdigest(), Size=len(data))]}))
            self.names += [package, feed]

    def run_manifest(self):
        return subprocess.run([sys.executable, str(Path(__file__).resolve().parents[1] / 'release-manifest.py'),
                               str(self.folder), '1.6.0'], capture_output=True, text=True)

    def test_complete_set_generates_verified_hashes_including_arch(self):
        result = self.run_manifest()
        self.assertEqual(0, result.returncode, result.stderr)
        lines = (self.folder / 'SHA256SUMS.txt').read_text().splitlines()
        self.assertEqual(20, len(lines))
        for line in lines:
            checksum, name = line.split('  ', 1)
            self.assertEqual(hashlib.sha256((self.folder / name).read_bytes()).hexdigest(), checksum)

    def test_missing_arch_or_other_installer_prevents_publication(self):
        for name in self.names:
            with self.subTest(name=name):
                file = self.folder / name
                data = file.read_bytes()
                file.unlink()
                self.assertNotEqual(0, self.run_manifest().returncode)
                self.assertFalse((self.folder / 'SHA256SUMS.txt').exists())
                file.write_bytes(data)

    def test_unexpected_file_is_not_included_in_release(self):
        (self.folder / 'private.db').write_bytes(b'fictitious')
        self.assertNotEqual(0, self.run_manifest().returncode)
        self.assertFalse((self.folder / 'SHA256SUMS.txt').exists())


if __name__ == '__main__':
    unittest.main()
