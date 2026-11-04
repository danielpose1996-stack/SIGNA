import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest


spec = importlib.util.spec_from_file_location('debian_package', Path(__file__).resolve().parents[1] / 'debian-package.py')
debian = importlib.util.module_from_spec(spec)
spec.loader.exec_module(debian)
CONTROL = '''Package: signa
Version: 1.6.5
Architecture: amd64
Depends: libc6 (>= 2.39), libicu74, libssl3t64, libx11-6
Description: SIGNA
 Aplicacion de escritorio.
'''


class DebianPackageTests(unittest.TestCase):
    def test_repack_changes_only_icu_without_changing_identity_or_version(self):
        for arch in ('amd64', 'arm64'):
            original = CONTROL.replace('amd64', arch)
            changed = debian.debian_control(original, '1.6.5', arch)
            self.assertEqual(original.replace('libicu74', 'libicu76'), changed)

    def test_wrong_version_architecture_identity_or_dependency_is_rejected(self):
        for original in (CONTROL.replace('1.6.5', '1.6.4'), CONTROL.replace('amd64', 'arm64'),
                         CONTROL.replace('signa', 'other'), CONTROL.replace('libicu74', 'libicu72')):
            with self.subTest(original=original), self.assertRaises(ValueError):
                debian.debian_control(original, '1.6.5', 'amd64')

    def test_original_download_must_match_the_published_checksum(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / 'SIGNA-1.6.5-linux-x64.deb'
            source.write_bytes(b'fictitious package')
            manifest = Path(directory) / 'SHA256SUMS.txt'
            digest = hashlib.sha256(source.read_bytes()).hexdigest()
            manifest.write_text(f'{digest}  {source.name}\n', encoding='utf-8')
            self.assertEqual(digest, debian.verify_source(source, manifest))
            source.write_bytes(b'corrupt download')
            with self.assertRaises(ValueError):
                debian.verify_source(source, manifest)

    def test_missing_or_duplicate_source_checksum_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / 'source.deb'
            source.write_bytes(b'fictitious')
            manifest = Path(directory) / 'SHA256SUMS.txt'
            line = f'{hashlib.sha256(source.read_bytes()).hexdigest()}  {source.name}\n'
            for text in ('', line + line):
                manifest.write_text(text, encoding='utf-8')
                with self.assertRaises(ValueError):
                    debian.verify_source(source, manifest)

    def test_payload_comparison_detects_any_binary_or_permission_change(self):
        with tempfile.TemporaryDirectory() as directory:
            first = Path(directory) / 'first'
            second = Path(directory) / 'second'
            first.mkdir(); second.mkdir()
            (first / 'program').write_bytes(b'original binary')
            (second / 'program').write_bytes(b'original binary')
            debian.compare_payload(first, second)
            (second / 'program').write_bytes(b'changed binary')
            with self.assertRaises(ValueError):
                debian.compare_payload(first, second)


if __name__ == '__main__':
    unittest.main()
