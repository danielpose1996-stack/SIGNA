import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path


class VelopackArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name) / 'source'
        self.source.mkdir()
        self.output = Path(self.temp.name) / 'output'

    def fixture(self, extra=None, bad_hash=False, channel='win-x64'):
        name = 'SIGNA-1.6.0-win-x64-full.nupkg'
        with zipfile.ZipFile(self.source / name, 'w') as archive:
            archive.writestr('SIGNA.nuspec', f'<package><metadata><id>SIGNA</id><version>1.6.0</version><channel>{channel}</channel><rid>{channel}</rid><os>win</os><machineArchitecture>x64</machineArchitecture></metadata></package>')
            archive.writestr('lib/app/SIGNA.exe', b'fictitious')
            if extra:
                archive.writestr(extra, b'fictitious')
        data = (self.source / name).read_bytes()
        feed = {'Assets': [dict(PackageId='SIGNA', Version='1.6.0', Type='Full', FileName=name,
                                SHA256='0' * 64 if bad_hash else hashlib.sha256(data).hexdigest(), Size=len(data))]}
        (self.source / 'releases.win-x64.json').write_text(json.dumps(feed))

    def run_collector(self):
        return subprocess.run([sys.executable, str(Path(__file__).resolve().parents[1] / 'collect-velopack.py'),
                               str(self.source), str(self.output), 'win-x64', '1.6.0'], capture_output=True)

    def test_valid_feed_is_collected_and_existing_output_is_not_overwritten(self):
        self.fixture()
        self.assertEqual(0, self.run_collector().returncode)
        self.assertEqual(2, len(list(self.output.iterdir())))
        self.assertNotEqual(0, self.run_collector().returncode)

    def test_wrong_hash_and_private_or_unsafe_paths_are_rejected_before_copy(self):
        for extra, bad_hash in [(None, True), ('lib/app/private.db', False), ('../outside', False), ('lib/app/backup.aulabk', False)]:
            with self.subTest(extra=extra, bad_hash=bad_hash):
                self.fixture(extra, bad_hash)
                self.assertNotEqual(0, self.run_collector().returncode)
                self.assertFalse(self.output.exists())

    def test_a_package_from_another_channel_is_rejected(self):
        self.fixture(channel='osx-x64')
        self.assertNotEqual(0, self.run_collector().returncode)
        self.assertFalse(self.output.exists())


if __name__ == '__main__':
    unittest.main()
