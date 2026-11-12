"""Validate tracked Unity asset identities without requiring the Unity Editor.

This deliberately does not claim to compile C# or verify Unity deserialization.
Run with Python 3 from any working directory.
"""
import json
import re
from pathlib import Path


def validate(root):
    assets = root / "Assets"
    guid_paths = {}
    count = 0
    for path in assets.rglob("*"):
        if path.suffix == ".meta":
            assert Path(str(path)[:-5]).exists(), f"Orphan meta: {path}"
            continue
        meta = Path(str(path) + ".meta")
        assert meta.is_file(), f"Missing meta: {path}"
        matches = re.findall(r"^guid: ([0-9a-f]{32})$", meta.read_text(), re.M)
        assert len(matches) == 1, f"Invalid guid: {meta}"
        guid = matches[0]
        assert guid not in guid_paths, f"Duplicate guid: {path} and {guid_paths.get(guid)}"
        guid_paths[guid] = path
        count += 1

    scenes = assets / "_MagnetEscape/Scenes"
    expected = ["StageSelect"] + [f"Stage{i}" for i in range(1, 5)]
    assert sorted(p.stem for p in scenes.glob("*.unity")) == sorted(expected)
    build = (root / "ProjectSettings/EditorBuildSettings.asset").read_text()
    entries = re.findall(r"- enabled: 1\s+path: (\S+)\s+guid: ([0-9a-f]{32})", build)
    assert [Path(p).stem for p, _ in entries] == expected, "Incorrect scene build order"
    for stage, name in enumerate(expected):
        scene = scenes / (name + ".unity")
        content = scene.read_text()
        assert re.search(rf"^  stage: {stage}$", content, re.M), f"Wrong stage id: {name}"
        ids = re.findall(r"^--- !u!\d+ &(\d+)$", content, re.M)
        assert len(ids) == len(set(ids)), f"Duplicate local object id: {name}"
        for file_id in re.findall(r"\{fileID: (\d+)\}", content):
            assert file_id == "0" or file_id in ids, f"Missing local reference: {name}: {file_id}"
        for guid in re.findall(r"guid: ([0-9a-f]{32})", content):
            target = guid_paths.get(guid)
            assert target is not None, f"Missing script GUID: {name}: {guid}"
            assert target.name == "StageBootstrap.cs", f"Unexpected bootstrap: {target}"
        path, build_guid = entries[stage]
        assert guid_paths[build_guid] == root / path, f"Build scene GUID mismatch: {name}"

    manifest = json.loads((root / "Packages/manifest.json").read_text())
    assert "com.unity.modules.physics2d" in manifest["dependencies"]
    assert "com.unity.test-framework" in manifest["dependencies"]
    definitions = list(assets.rglob("*.asmdef"))
    names = {json.loads(p.read_text())["name"] for p in definitions}
    assert len(names) == len(definitions), "Duplicate assembly name"
    for p in definitions:
        for reference in json.loads(p.read_text()).get("references", []):
            assert reference in names, f"Missing assembly: {reference}"
    tags = (root / "ProjectSettings/TagManager.asset").read_text()
    layers = tags.split("  layers:\n", 1)[1].split("  m_SortingLayers:", 1)[0]
    assert len(layers.splitlines()) == 32, "Expected 32 Unity layers"
    assert layers.splitlines()[8].strip() == "- Ground", "Ground layer configuration missing"
    print(f"PASS: {count} asset identities, 5 scene entries, script/local references, assemblies, packages, Ground layer")
    print("Unity import, C# compilation and play tests remain unverified.")


if __name__ == "__main__":
    validate(Path(__file__).resolve().parents[1])
