import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from unity_tasks import inspect_test_report

SCRIPT = Path(__file__).resolve().parents[1] / 'unity_tasks.py'
FAKE = '''#!/usr/bin/env python3
import os
from pathlib import Path
import sys
args = sys.argv[1:]
if os.environ.get('FAKE_FAIL'):
    sys.exit(2)
if os.environ.get('FAKE_NO_REPORT'):
    sys.exit(0)
if '-runTests' in args:
    path = Path(args[args.index('-testResults') + 1])
    xml = '<test-run result="Passed" total="1" failed="0"><test-case result="Passed" /></test-run>'
    if os.environ.get('FAKE_EMPTY'):
        xml = '<test-run result="Passed" total="0" failed="0" />'
    path.write_text(xml)
else:
    path = Path(args[args.index('-magnetBuildPath') + 1])
    (path / 'Contents').mkdir(parents=True)
    (path / 'Contents/Info.plist').write_text('<plist />')
    path.with_suffix('.build.txt').write_text('Result: Succeeded')
'''


class UnityTaskTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='magnet-unity-cli-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.editor = self.root / 'Fake Unity'
        self.editor.write_text(FAKE)
        self.editor.chmod(0o755)
        self.output = self.root / 'Results With Spaces'

    def report(self, content):
        path = self.root / 'test.xml'
        path.write_text(content)
        return path

    def run_cli(self, task='test', extra=(), flags=None):
        env = os.environ.copy()
        env.update(flags or {})
        command = [sys.executable, str(SCRIPT), task, '--unity', str(self.editor), '--output', str(self.output), *extra]
        return subprocess.run(command, env=env, text=True, capture_output=True, timeout=20)

    def test_real_report_must_have_a_passing_case(self):
        path = self.report('<test-run result="Passed" failed="0"><test-case result="Passed" /></test-run>')
        self.assertEqual(inspect_test_report(path), 1)

    def test_empty_report_is_not_success(self):
        path = self.report('<test-run result="Passed" total="0" />')
        with self.assertRaisesRegex(ValueError, 'No passing tests'):
            inspect_test_report(path)

    def test_failed_report_is_not_success(self):
        path = self.report('<test-run result="Failed" failed="1"><test-case result="Passed" /><test-case result="Failed" /></test-run>')
        with self.assertRaisesRegex(ValueError, 'test failures'):
            inspect_test_report(path)

    def test_skipped_only_report_is_not_success(self):
        path = self.report('<test-run result="Passed"><test-case result="Skipped" /></test-run>')
        with self.assertRaisesRegex(ValueError, 'No passing tests'):
            inspect_test_report(path)

    def test_two_suites_use_paths_with_spaces_safely(self):
        result = self.run_cli()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(len(list(self.output.glob('*/EditMode.xml'))), 1)
        self.assertEqual(len(list(self.output.glob('*/PlayMode.xml'))), 1)

    def test_nonzero_unity_exit_stops_before_second_suite(self):
        result = self.run_cli(flags={'FAKE_FAIL': '1'})
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Unity exited with code 2', result.stderr)
        self.assertFalse(list(self.output.glob('*/PlayMode.xml')))

    def test_unity_exit_zero_with_no_report_is_failure(self):
        result = self.run_cli(flags={'FAKE_NO_REPORT': '1'})
        self.assertNotEqual(result.returncode, 0)

    def test_cli_empty_report_is_failure(self):
        result = self.run_cli(flags={'FAKE_EMPTY': '1'})
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('No passing tests', result.stderr)

    def test_build_checks_for_app_and_build_summary(self):
        result = self.run_cli('build-mac', ['--development'])
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue(list(self.output.glob('*/MagnetEscape.app/Contents/Info.plist')))
        self.assertTrue(list(self.output.glob('*/MagnetEscape.build.txt')))

    def test_build_exit_zero_with_no_output_is_failure(self):
        result = self.run_cli('build-mac', flags={'FAKE_NO_REPORT': '1'})
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('without the app and build report', result.stderr)

    def test_dry_run_does_not_create_artifacts_or_invoke_unity(self):
        result = self.run_cli(extra=['--dry-run'], flags={'FAKE_FAIL': '1'})
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('DRY RUN ONLY', result.stdout)
        self.assertFalse(self.output.exists())


if __name__ == '__main__':
    unittest.main()
