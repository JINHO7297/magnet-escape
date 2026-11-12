using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagnetEscape.Tests
{
    public sealed class SceneReferenceTests
    {
        [TestCase("StageSelect", 0)]
        [TestCase("Stage1", 1)]
        [TestCase("Stage2", 2)]
        [TestCase("Stage3", 3)]
        [TestCase("Stage4", 4)]
        public void SceneDeserializesWithCorrectBootstrap(string sceneName, int stage)
        {
            string path = "Assets/_MagnetEscape/Scenes/" + sceneName + ".unity";
            Scene existing = SceneManager.GetSceneByPath(path);
            bool alreadyLoaded = existing.IsValid() && existing.isLoaded;
            Scene scene = alreadyLoaded ? existing : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                StageBootstrap[] bootstraps = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<StageBootstrap>(true)).ToArray();
                Assert.That(bootstraps.Length, Is.EqualTo(1));
                SerializedObject serialized = new SerializedObject(bootstraps[0]);
                Assert.That(serialized.FindProperty("stage").intValue, Is.EqualTo(stage));
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero);
            }
            finally
            {
                if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BuildStartsAtSelectionAndIncludesFourStages()
        {
            string[] expected = { "StageSelect", "Stage1", "Stage2", "Stage3", "Stage4" };
            string[] actual = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path)).ToArray();
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
