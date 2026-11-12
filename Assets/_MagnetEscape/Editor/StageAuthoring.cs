using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagnetEscape.Editor
{
    // Optional authoring workflow. Source scenes and their GUIDs are never overwritten.
    public static class StageAuthoring
    {
        private const string Root = "Assets/_MagnetEscape";
        private const string Editable = Root + "/Scenes/Editable";
        private const string Generated = Root + "/Generated";

        [MenuItem("Tools/Magnet Escape/Create Editable Stage Copies")]
        public static void CreateCopies()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode before creating editable stages.");
                return;
            }
            Directory.CreateDirectory(Editable);
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            int created = 0;
            for (int stage = 1; stage <= 4; stage++)
            {
                string path = Editable + "/Stage" + stage + ".unity";
                if (File.Exists(path))
                {
                    Debug.Log("Preserving existing editable scene: " + path);
                    continue;
                }
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    GameObject root = new GameObject("Stage Runtime");
                    StageBootstrap bootstrap = root.AddComponent<StageBootstrap>();
                    SerializedObject serialized = new SerializedObject(bootstrap);
                    serialized.FindProperty("stage").intValue = stage;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    bootstrap.BuildScene();
                    Object.DestroyImmediate(bootstrap);
                    PersistGeneratedMaterials(scene);
                    if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save " + path);
                    created++;
                }
                finally
                {
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created " + created + " editable Magnet Escape stages. Existing scenes were preserved. "
                + "Open Scenes/Editable, then choose 'Use Editable Stages in Build' when ready.");
        }

        private static void PersistGeneratedMaterials(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material material = renderer.sharedMaterial;
                    if (material == null || EditorUtility.IsPersistent(material)) continue;
                    string path = Generated + "/MagnetEffects.mat";
                    Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (existing != null) renderer.sharedMaterial = existing;
                    else AssetDatabase.CreateAsset(material, path);
                }
                foreach (Rigidbody2D body in root.GetComponentsInChildren<Rigidbody2D>(true))
                {
                    PhysicsMaterial2D material = body.sharedMaterial;
                    if (material == null || EditorUtility.IsPersistent(material)) continue;
                    string path = Generated + "/RobotFrictionless.physicsMaterial2D";
                    PhysicsMaterial2D existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
                    if (existing != null) body.sharedMaterial = existing;
                    else AssetDatabase.CreateAsset(material, path);
                }
            }
        }

        [MenuItem("Tools/Magnet Escape/Use Editable Stages in Build")]
        public static void UseEditableStages()
        {
            string[] stages = Enumerable.Range(1, 4).Select(stage => Editable + "/Stage" + stage + ".unity").ToArray();
            if (stages.Any(path => !File.Exists(path)))
            {
                Debug.LogWarning("Create all four editable stage copies first. Build Settings were not changed.");
                return;
            }
            string selection = Root + "/Scenes/StageSelect.unity";
            EditorBuildSettings.scenes = new[] { selection }.Concat(stages)
                .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            Debug.Log("Build Settings now use editable stage copies. Source scene GUIDs are unchanged.");
        }

        [MenuItem("Tools/Magnet Escape/Use Generated Source Stages in Build")]
        public static void UseSourceStages()
        {
            string[] names = { "StageSelect", "Stage1", "Stage2", "Stage3", "Stage4" };
            EditorBuildSettings.scenes = names.Select(name => new EditorBuildSettingsScene(Root + "/Scenes/" + name + ".unity", true)).ToArray();
        }

        [MenuItem("Tools/Magnet Escape/Create Player Prefab from Selected Robot")]
        public static void CreatePlayerPrefab()
        {
            GameObject selected = Selection.activeGameObject;
            if (EditorApplication.isPlayingOrWillChangePlaymode || selected == null || selected.GetComponent<PlayerMotor>() == null)
            {
                Debug.LogWarning("In Edit mode, select the robot in an editable stage copy.");
                return;
            }
            const string path = Root + "/Prefabs/MagnetRobot.prefab";
            if (File.Exists(path))
            {
                Debug.LogWarning("Preserving the existing robot prefab: " + path);
                return;
            }
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            PrefabUtility.SaveAsPrefabAsset(selected, path);
            Debug.Log("Created robot prefab. Stage-specific dash and magnet flags remain serialized.");
        }
    }
}
