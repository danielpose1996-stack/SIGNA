import hashlib
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class ReleaseManifestTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.folder = Path(self.temporary.name)
        self.names = [
            'SIGNA-1.5.0-windows-x64-setup.exe',
            'SIGNA-1.5.0-linux-x64.deb', 'SIGNA-1.5.0-linux-x64.rpm',
            'SIGNA-1.5.0-linux-arm64.deb', 'SIGNA-1.5.0-linux-arm64.rpm',
            'SIGNA-1.5.0-osx-x64.dmg', 'SIGNA-1.5.0-osx-arm64.dmg',
            'SIGNA-1.5.0-archlinux-x64.pkg.tar.zst',
        ]
        for name in self.names:
            (self.folder / name).write_bytes(b'fixture-installer\n' * 60000)

    def run_manifest(self):
        return subprocess.run([sys.executable, str(Path(__file__).resolve().parents[1] / 'release-manifest.py'),
                               str(self.folder), '1.5.0'], capture_output=True, text=True)

    def test_complete_set_generates_verified_hashes_including_arch(self):
        result = self.run_manifest()
        self.assertEqual(0, result.returncode, result.stderr)
        lines = (self.folder / 'SHA256SUMS.txt').read_text().splitlines()
        self.assertEqual(8, len(lines))
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
