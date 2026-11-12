import contextlib
import io
import json
from pathlib import Path
import re
import shutil
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from validate_project import validate


class ProjectIntegrityTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="magnet-integrity-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        source = Path(__file__).resolve().parents[2]
        for folder in ["Assets", "Packages", "ProjectSettings"]:
            shutil.copytree(source / folder, self.root / folder)
        self.assets = self.root / "Assets/_MagnetEscape"

    def check(self):
        with contextlib.redirect_stdout(io.StringIO()):
            validate(self.root)

    def test_repository_passes(self):
        self.check()

    def test_missing_metadata_is_rejected(self):
        (self.assets / "Scripts/PlayerMotor.cs.meta").unlink()
        with self.assertRaisesRegex(AssertionError, "Missing meta"):
            self.check()

    def test_duplicate_guid_is_rejected(self):
        source = self.assets / "Scripts/PlayerMotor.cs.meta"
        target = self.assets / "Scripts/Checkpoint.cs.meta"
        guid = re.search(r"guid: (\w+)", source.read_text())[1]
        target.write_text(re.sub(r"guid: \w+", "guid: " + guid, target.read_text()))
        with self.assertRaisesRegex(AssertionError, "Duplicate guid"):
            self.check()

    def test_missing_scene_script_is_rejected(self):
        scene = self.assets / "Scenes/Stage1.unity"
        scene.write_text(re.sub(r"guid: \w+", "guid: " + "0" * 32, scene.read_text()))
        with self.assertRaisesRegex(AssertionError, "Missing script GUID"):
            self.check()

    def test_wrong_build_scene_identity_is_rejected(self):
        settings = self.root / "ProjectSettings/EditorBuildSettings.asset"
        first = re.search(r"guid: (\w+)", (self.assets / "Scenes/Stage1.unity.meta").read_text())[1]
        other = re.search(r"guid: (\w+)", (self.assets / "Scenes/Stage2.unity.meta").read_text())[1]
        settings.write_text(settings.read_text().replace(first, other))
        with self.assertRaisesRegex(AssertionError, "Build scene GUID mismatch"):
            self.check()

    def test_missing_ground_layer_is_rejected(self):
        tags = self.root / "ProjectSettings/TagManager.asset"
        tags.write_text(tags.read_text().replace("- Ground", "- WrongLayer"))
        with self.assertRaisesRegex(AssertionError, "Ground layer"):
            self.check()

    def test_missing_runtime_sprite_is_rejected(self):
        (self.assets / "Resources/Art/robot_idle.png").unlink()
        (self.assets / "Resources/Art/robot_idle.png.meta").unlink()
        with self.assertRaisesRegex(AssertionError, "Missing runtime sprite"):
            self.check()

    def test_corrupted_sprite_is_rejected(self):
        sprite = self.assets / "Resources/Art/robot_idle.png"
        data = bytearray(sprite.read_bytes())
        data[20] ^= 1
        sprite.write_bytes(data)
        with self.assertRaisesRegex(AssertionError, "PNG checksum mismatch"):
            self.check()

    def test_wrong_sprite_import_mode_is_rejected(self):
        meta = self.assets / "Resources/Art/platform.png.meta"
        meta.write_text(meta.read_text().replace("textureType: 8", "textureType: 0"))
        with self.assertRaisesRegex(AssertionError, "Invalid sprite import setting"):
            self.check()

    def test_editor_dependency_in_runtime_is_rejected(self):
        path = self.assets / "Scripts/MagnetEscape.Runtime.asmdef"
        assembly = json.loads(path.read_text())
        assembly["references"] = ["MagnetEscape.Editor"]
        path.write_text(json.dumps(assembly))
        with self.assertRaisesRegex(AssertionError, "Player assembly .* references Editor-only"):
            self.check()


if __name__ == "__main__":
    unittest.main()
