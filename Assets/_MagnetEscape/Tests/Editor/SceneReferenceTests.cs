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
        public void FestivalCatalogHasTwelveDistinctCoursesWithStoriesAndShapingRoutes()
        {
            Assert.That(StageInfo.Count, Is.EqualTo(12));
            for (int i = 1; i <= StageInfo.Count; i++)
            {
                StageDefinition layout = StageInfo.Definition(i);
                Assert.That(layout, Is.Not.Null);
                Assert.That(layout.Number, Is.EqualTo(i));
                Assert.That(string.IsNullOrEmpty(layout.OpeningMessage), Is.False, "Missing opening for stage " + i);
                Assert.That(layout.EndingMessage, Is.Not.Empty);
                Assert.That(StageInfo.NextStage(i), Is.EqualTo(i % 4 == 0 ? 0 : i + 1));
                if (i > 4) Assert.That(layout.Routes.Length, Is.GreaterThanOrEqualTo(4));
                Assert.That(StageRecords.BuildScenePath(i), Does.EndWith("/Stage" + i + ".unity"));
            }
            for (int slot = 1; slot <= StageInfo.FestivalCount; slot++)
            {
                int stage = StageInfo.FestivalForSlot(slot);
                Assert.That(StageInfo.NextStage(stage), Is.Zero);
                Assert.That(StageInfo.Definition(stage).MagnetEnabled, Is.True);
            }
            Assert.That(StageInfo.FestivalForSlot(1), Is.Not.EqualTo(StageInfo.FestivalForSlot(2)));
        }

        [Test]
        public void GroundLayerIsAvailableToRuntimePhysicsQueries()
        {
            Assert.That(LayerMask.NameToLayer("Ground"), Is.GreaterThanOrEqualTo(0));
            Assert.That(LayerMask.GetMask("Ground"), Is.Not.Zero);
        }

        [Test]
        public void BuildStartsAtSelectionAndIncludesFourStages()
        {
            string[] expected = new[] { "StageSelect" }.Concat(Enumerable.Range(1, StageInfo.Count).Select(n => "Stage" + n)).Concat(new[] { "Endless" }).ToArray();
            string[] actual = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path)).ToArray();
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
