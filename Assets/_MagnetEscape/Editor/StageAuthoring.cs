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
        public static void CreateCopies() => CreateCopiesAt(Editable);

        [MenuItem("Tools/Magnet Escape/Create Fresh Course Revision")]
        public static void CreateRevisionCopies()
        {
            string revision = "Revision_" + System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")
                + "_UTC_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
            string folder = Root + "/Scenes/Revisions/" + revision;
            if (!CreateCopiesAt(folder)) return;
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private static bool CreateCopiesAt(string folder)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode before creating editable stages.");
                return false;
            }
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            int created = 0;
            for (int stage = 1; stage <= 4; stage++)
            {
                string path = folder + "/Stage" + stage + ".unity";
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
                    bootstrap.BuildScene(prefab => (GameObject)PrefabUtility.InstantiatePrefab(prefab));
                    Object.DestroyImmediate(bootstrap);
                    PersistGeneratedMaterials(scene);
                    PlayerMotor player = root.GetComponent<GameSession>().Player;
                    if (player != null && PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(player.gameObject);
                        foreach (Component component in player.GetComponents<Component>())
                            if (component != null) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    }
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
            Debug.Log("Created " + created + " editable stages in " + folder + ". Existing scenes and Build Settings were preserved.");
            return true;
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
        public static void UseEditableStages() => UseStageFolder(Editable);

        [MenuItem("Tools/Magnet Escape/Use Selected Revision in Build")]
        public static void UseSelectedRevision()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Select a generated course revision folder or a scene inside it.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(path)) path = Path.GetDirectoryName(path);
            path = path?.Replace('\\', '/');
            if (string.IsNullOrEmpty(path) || !path.StartsWith(Root + "/Scenes/Revisions/", System.StringComparison.Ordinal))
            {
                Debug.LogWarning("Select a generated course revision folder or a scene inside it. Build Settings were not changed.");
                return;
            }
            UseStageFolder(path);
        }

        private static void UseStageFolder(string folder)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode before changing the course build set.");
                return;
            }
            string[] stages = Enumerable.Range(1, 4).Select(stage => folder + "/Stage" + stage + ".unity").ToArray();
            if (stages.Any(path => !File.Exists(path)))
            {
                Debug.LogWarning("The selected folder must contain Stage1-Stage4. Build Settings were not changed.");
                return;
            }
            string selection = Root + "/Scenes/StageSelect.unity";
            EditorBuildSettings.scenes = new[] { selection }.Concat(stages)
                .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
            Debug.Log("Build Settings now use " + folder + ". Source scenes and previous revisions were preserved.");
        }

        [MenuItem("Tools/Magnet Escape/Use Generated Source Stages in Build")]
        public static void UseSourceStages()
        {
            UseStageFolder(Root + "/Scenes");
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
