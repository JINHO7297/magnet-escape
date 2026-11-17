using NUnit.Framework;

namespace MagnetEscape.Tests
{
    public sealed class RankTests
    {
        [Test]
        public void RunnerRecordsStaySeparateFromLegacyAndKeepIndependentBestTimeAndRank()
        {
            const string runner = "MagnetSkater.v3.Stage1";
            const string previousRunner = "MagnetSkater.v2.Stage1";
            const string legacy = "MagnetEscape.Stage1";
            const string scene = "Assets/_MagnetEscape/Scenes/Stage1.unity";
            var times = new System.Collections.Generic.Dictionary<string, float>();
            var ints = new System.Collections.Generic.Dictionary<string, int>();
            foreach (string prefix in new[] { runner, legacy, previousRunner })
            {
                if (UnityEngine.PlayerPrefs.HasKey(prefix + ".Time")) times[prefix + ".Time"] = UnityEngine.PlayerPrefs.GetFloat(prefix + ".Time");
                foreach (string suffix in new[] { ".TimeDeaths", ".Rank" })
                    if (UnityEngine.PlayerPrefs.HasKey(prefix + suffix)) ints[prefix + suffix] = UnityEngine.PlayerPrefs.GetInt(prefix + suffix);
            }
            try
            {
                UnityEngine.PlayerPrefs.SetFloat(legacy + ".Time", 3f);
                UnityEngine.PlayerPrefs.SetFloat(previousRunner + ".Time", 1f);
                UnityEngine.PlayerPrefs.SetInt(legacy + ".Rank", 4);
                StageRecords.ClearCourse(1, scene);
                Assert.That(StageRecords.ForScene(1, scene).HasTime, Is.False);
                StageRecords.Save(1, scene, 12f, 1, 3);
                StageRecords.Save(1, scene, 15f, 0, 4);
                StageRecords.Record result = StageRecords.ForScene(1, scene);
                Assert.That(result.BestTime, Is.EqualTo(12f));
                Assert.That(UnityEngine.PlayerPrefs.GetFloat(runner + ".Time", -1f), Is.EqualTo(12f));
                Assert.That(result.Deaths, Is.EqualTo(1));
                Assert.That(result.BestRank, Is.EqualTo(4));
                StageRecords.ClearCourse(1, scene);
                Assert.That(StageRecords.ForScene(1, scene).HasTime, Is.False);
                Assert.That(UnityEngine.PlayerPrefs.GetFloat(legacy + ".Time"), Is.EqualTo(3f));
                Assert.That(UnityEngine.PlayerPrefs.GetFloat(previousRunner + ".Time"), Is.EqualTo(1f));
                Assert.That(UnityEngine.PlayerPrefs.GetInt(legacy + ".Rank"), Is.EqualTo(4));
            }
            finally
            {
                foreach (string prefix in new[] { runner, legacy, previousRunner })
                    foreach (string suffix in new[] { ".Time", ".TimeDeaths", ".Rank" }) UnityEngine.PlayerPrefs.DeleteKey(prefix + suffix);
                foreach (var pair in times) UnityEngine.PlayerPrefs.SetFloat(pair.Key, pair.Value);
                foreach (var pair in ints) UnityEngine.PlayerPrefs.SetInt(pair.Key, pair.Value);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        [Test]
        public void EndlessLeaderboardKeepsOnlyFiveBestScoresAndRejectsInvalidRuns()
        {
            string key = EndlessRecords.Key;
            bool existed = UnityEngine.PlayerPrefs.HasKey(key);
            string saved = UnityEngine.PlayerPrefs.GetString(key);
            try
            {
                UnityEngine.PlayerPrefs.DeleteKey(key);
                foreach (int score in new[] { 200, 100, 900, 300, 800, 700, 600 })
                    EndlessRecords.Save(score, score / 10f, 0, 42);
                var runs = EndlessRecords.Load();
                Assert.That(runs.Count, Is.EqualTo(5));
                Assert.That(runs[0].Score, Is.EqualTo(900));
                Assert.That(runs[4].Score, Is.EqualTo(300));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => EndlessRecords.Save(-1, 1f, 0, 42));
                UnityEngine.PlayerPrefs.SetString(key, "invalid json");
                Assert.That(EndlessRecords.Load(), Is.Empty);
            }
            finally
            {
                if (existed) UnityEngine.PlayerPrefs.SetString(key, saved); else UnityEngine.PlayerPrefs.DeleteKey(key);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        [TestCase(42f, 0, 1, "S")]
        [TestCase(42.01f, 0, 1, "A")]
        [TestCase(10f, 1, 1, "A")]
        [TestCase(63f, 2, 1, "A")]
        [TestCase(63.01f, 2, 1, "B")]
        [TestCase(10f, 3, 1, "B")]
        [TestCase(84f, 5, 1, "B")]
        [TestCase(84.01f, 5, 1, "C")]
        [TestCase(10f, 6, 1, "C")]
        [TestCase(82f, 0, 4, "S")]
        public void RankUsesTimeAndDeaths(float seconds, int deaths, int stage, string expected)
        {
            Assert.That(GameSession.EvaluateRank(seconds, deaths, stage), Is.EqualTo(expected));
        }
    }
}
