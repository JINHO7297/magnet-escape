using NUnit.Framework;

namespace MagnetEscape.Tests
{
    public sealed class RankTests
    {
        [Test]
        public void RunnerRecordsStaySeparateFromLegacyAndKeepIndependentBestTimeAndRank()
        {
            const string runner = "MagnetSkater.Stage1";
            const string legacy = "MagnetEscape.Stage1";
            const string scene = "Assets/_MagnetEscape/Scenes/Stage1.unity";
            var times = new System.Collections.Generic.Dictionary<string, float>();
            var ints = new System.Collections.Generic.Dictionary<string, int>();
            foreach (string prefix in new[] { runner, legacy })
            {
                if (UnityEngine.PlayerPrefs.HasKey(prefix + ".Time")) times[prefix + ".Time"] = UnityEngine.PlayerPrefs.GetFloat(prefix + ".Time");
                foreach (string suffix in new[] { ".TimeDeaths", ".Rank" })
                    if (UnityEngine.PlayerPrefs.HasKey(prefix + suffix)) ints[prefix + suffix] = UnityEngine.PlayerPrefs.GetInt(prefix + suffix);
            }
            try
            {
                UnityEngine.PlayerPrefs.SetFloat(legacy + ".Time", 3f);
                UnityEngine.PlayerPrefs.SetInt(legacy + ".Rank", 4);
                StageRecords.ClearCourse(1, scene);
                Assert.That(StageRecords.ForScene(1, scene).HasTime, Is.False);
                StageRecords.Save(1, scene, 12f, 1, 3);
                StageRecords.Save(1, scene, 15f, 0, 4);
                StageRecords.Record result = StageRecords.ForScene(1, scene);
                Assert.That(result.BestTime, Is.EqualTo(12f));
                Assert.That(result.Deaths, Is.EqualTo(1));
                Assert.That(result.BestRank, Is.EqualTo(4));
                StageRecords.ClearCourse(1, scene);
                Assert.That(StageRecords.ForScene(1, scene).HasTime, Is.False);
                Assert.That(UnityEngine.PlayerPrefs.GetFloat(legacy + ".Time"), Is.EqualTo(3f));
                Assert.That(UnityEngine.PlayerPrefs.GetInt(legacy + ".Rank"), Is.EqualTo(4));
            }
            finally
            {
                foreach (string prefix in new[] { runner, legacy })
                    foreach (string suffix in new[] { ".Time", ".TimeDeaths", ".Rank" }) UnityEngine.PlayerPrefs.DeleteKey(prefix + suffix);
                foreach (var pair in times) UnityEngine.PlayerPrefs.SetFloat(pair.Key, pair.Value);
                foreach (var pair in ints) UnityEngine.PlayerPrefs.SetInt(pair.Key, pair.Value);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        [TestCase(22f, 0, 1, "S")]
        [TestCase(22.01f, 0, 1, "A")]
        [TestCase(10f, 1, 1, "A")]
        [TestCase(33f, 2, 1, "A")]
        [TestCase(33.01f, 2, 1, "B")]
        [TestCase(10f, 3, 1, "B")]
        [TestCase(44f, 5, 1, "B")]
        [TestCase(44.01f, 5, 1, "C")]
        [TestCase(10f, 6, 1, "C")]
        [TestCase(40f, 0, 4, "S")]
        public void RankUsesTimeAndDeaths(float seconds, int deaths, int stage, string expected)
        {
            Assert.That(GameSession.EvaluateRank(seconds, deaths, stage), Is.EqualTo(expected));
        }
    }
}
