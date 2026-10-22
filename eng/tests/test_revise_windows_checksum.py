import hashlib
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class WindowsChecksumRevisionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.installer = self.root / 'SIGNA-1.6.0-windows-x64-setup.exe'
        self.installer.write_bytes(b'MZ' + b'fictitious-installer' * 70000)
        self.original = self.root / 'original.txt'
        self.output = self.root / 'SHA256SUMS.txt'
        self.lines = ['a' * 64 + '  SIGNA-1.6.0-linux-x64.deb',
                      'b' * 64 + '  ' + self.installer.name,
                      'c' * 64 + '  SIGNA-1.6.0-win-x64-full.nupkg',
                      'd' * 64 + '  releases.win-x64.json']
        self.original.write_text('\n'.join(self.lines) + '\n')

    def run_revision(self):
        return subprocess.run([sys.executable, str(Path(__file__).resolve().parents[1] / 'revise-windows-checksum.py'),
                               str(self.original), str(self.installer), str(self.output), '1.6.0'], capture_output=True)

    def test_only_the_selected_installer_entry_changes_and_original_is_preserved(self):
        before = self.original.read_bytes()
        self.assertEqual(0, self.run_revision().returncode)
        lines = self.output.read_text().splitlines()
        expected = self.lines.copy()
        expected[1] = hashlib.sha256(self.installer.read_bytes()).hexdigest() + '  ' + self.installer.name
        self.assertEqual(expected, lines)
        self.assertEqual(before, self.original.read_bytes())
        self.assertNotEqual(0, self.run_revision().returncode)  # No sobrescribir manifiestos existentes.

    def test_missing_duplicate_unsafe_or_invalid_manifest_is_rejected(self):
        cases = [self.lines[:1], self.lines + self.lines[1:2],
                 self.lines + ['e' * 64 + '  ../private.db'], self.lines + ['no-es-un-hash  archivo']]
        for lines in cases:
            with self.subTest(lines=lines):
                self.original.write_text('\n'.join(lines) + '\n')
                self.assertNotEqual(0, self.run_revision().returncode)
                self.assertFalse(self.output.exists())

    def test_renamed_non_executable_or_truncated_candidate_is_rejected(self):
        for payload in (b'PDF' + b'x' * 1100000, b'MZsmall'):
            with self.subTest(length=len(payload)):
                self.installer.write_bytes(payload)
                self.assertNotEqual(0, self.run_revision().returncode)
                self.assertFalse(self.output.exists())
        self.installer.write_bytes(b'MZ' + b'x' * 1100000)
        self.installer = self.installer.rename(self.root / 'otra-version.exe')
        self.assertNotEqual(0, self.run_revision().returncode)


if __name__ == '__main__':
    unittest.main()
