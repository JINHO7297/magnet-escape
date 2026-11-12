"""Run real Unity tests or a macOS build with isolated, inspectable artifacts.

Uses only Python's standard library. --dry-run prints commands without running Unity.
"""
import argparse
from datetime import datetime, timezone
import os
from pathlib import Path
import re
import shlex
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET

PROJECT = Path(__file__).resolve().parents[1]


def editor_path(explicit=None):
    if explicit or os.environ.get('UNITY_EDITOR'):
        return Path(explicit or os.environ['UNITY_EDITOR']).expanduser()
    version_file = (PROJECT / 'ProjectSettings/ProjectVersion.txt').read_text()
    match = re.search(r'^m_EditorVersion: (\S+)$', version_file, re.M)
    if not match:
        raise ValueError('ProjectVersion.txt does not declare a Unity version.')
    return Path('/Applications/Unity/Hub/Editor') / match[1] / 'Unity.app/Contents/MacOS/Unity'


def inspect_test_report(path):
    root = ET.parse(path).getroot()
    cases = list(root.iter('test-case'))
    if not cases or int(root.get('total', str(len(cases)))) <= 0 or not any(case.get('result') == 'Passed' for case in cases):
        raise ValueError(f'No passing tests were executed: {path}')
    if root.get('result') != 'Passed' or int(root.get('failed', '0')) != 0:
        raise ValueError(f'Unity reported test failures: {path}')
    if any(case.get('result') in {'Failed', 'Error', 'Inconclusive'} for case in cases):
        raise ValueError(f'Unity reported unsuccessful test cases: {path}')
    return len(cases)


def invoke(command):
    print(shlex.join(str(value) for value in command), flush=True)
    result = subprocess.run([str(value) for value in command], check=False)
    if result.returncode:
        raise RuntimeError(f'Unity exited with code {result.returncode}. See the log file in the command above.')


def main(argv=None):
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument('--unity', help='Unity executable; falls back to UNITY_EDITOR, then the macOS Hub path.')
    common.add_argument('--output', type=Path, help='Artifact root. Each actual run gets a unique subdirectory.')
    common.add_argument('--dry-run', action='store_true', help='Print commands only; do not run Unity or claim success.')
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='task', required=True)
    test = commands.add_parser('test', parents=[common])
    test.add_argument('--suite', choices=['all', 'EditMode', 'PlayMode'], default='all')
    build = commands.add_parser('build-mac', parents=[common])
    build.add_argument('--development', action='store_true')
    args = parser.parse_args(argv)
    try:
        editor = editor_path(args.unity)
        if not args.dry_run and (not editor.is_file() or not os.access(editor, os.X_OK)):
            raise FileNotFoundError(f'Unity executable unavailable: {editor}. Install the project version or provide --unity.')
        default = PROJECT / ('artifacts/tests' if args.task == 'test' else 'Builds')
        artifact_root = (args.output or default).expanduser().resolve()
        run = 'DRY_RUN' if args.dry_run else datetime.now(timezone.utc).strftime('%Y%m%d_%H%M%S_UTC_') + uuid.uuid4().hex[:8]
        directory = artifact_root / run
        if not args.dry_run:
            directory.mkdir(parents=True, exist_ok=False)
        base = [editor, '-batchmode', '-nographics', '-projectPath', PROJECT]
        if args.task == 'test':
            suites = ['EditMode', 'PlayMode'] if args.suite == 'all' else [args.suite]
            for suite in suites:
                result_file = directory / (suite + '.xml')
                command = base + ['-runTests', '-testPlatform', suite, '-testResults', result_file, '-logFile', directory / (suite + '.log')]
                if args.dry_run:
                    print(shlex.join(str(value) for value in command))
                else:
                    invoke(command)
                    count = inspect_test_report(result_file)
                    print(f'{suite}: Unity report passed, {count} cases; {result_file}')
        else:
            app = directory / 'MagnetEscape.app'
            command = base + ['-quit', '-buildTarget', 'StandaloneOSX', '-executeMethod',
                              'MagnetEscape.Editor.MacBuild.BuildFromCommandLine',
                              '-magnetBuildPath', app, '-logFile', directory / 'build.log']
            if args.development:
                command.append('-magnetDevelopment')
            if args.dry_run:
                print(shlex.join(str(value) for value in command))
            else:
                invoke(command)
                if not (app / 'Contents/Info.plist').is_file() or not app.with_suffix('.build.txt').is_file():
                    raise ValueError(f'Unity returned success without the app and build report: {app}')
                print(f'Built macOS app: {app}')
        if args.dry_run:
            print('DRY RUN ONLY: Unity was not executed. No tests or build were verified.')
        return 0
    except (OSError, RuntimeError, ValueError, ET.ParseError) as error:
        print(str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
