using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagnetEscape
{
    // Record sets follow the actual build scene, so edited revisions do not share scores.
    // The original five source scenes retain their existing PlayerPrefs keys.
    public static class StageRecords
    {
        public readonly struct Record
        {
            public readonly bool HasTime;
            public readonly float BestTime;
            public readonly int Deaths;
            public readonly int BestRank;

            public Record(string key)
            {
                BestTime = PlayerPrefs.GetFloat(key + ".Time", -1f);
                HasTime = PlayerPrefs.HasKey(key + ".Time") && BestTime >= 0f
                    && !float.IsNaN(BestTime) && !float.IsInfinity(BestTime);
                Deaths = Mathf.Max(0, PlayerPrefs.GetInt(key + ".TimeDeaths", 0));
                BestRank = Mathf.Clamp(PlayerPrefs.GetInt(key + ".Rank", 0), 0, 4);
            }
        }

        public readonly struct SavedRun
        {
            public readonly float PreviousTime;
            public readonly bool NewTime;
            public readonly bool NewRank;

            public SavedRun(float previousTime, bool newTime, bool newRank)
            {
                PreviousTime = previousTime;
                NewTime = newTime;
                NewRank = newRank;
            }
        }

        public static string BuildScenePath(int stage)
        {
            string name = "Stage" + stage;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (Path.GetFileNameWithoutExtension(path) == name) return path;
            }
            return SourcePath(stage);
        }

        public static Record ForBuildStage(int stage) => new Record(Key(stage, BuildScenePath(stage)));

        public static SavedRun Save(int stage, string scenePath, float seconds, int deaths, int rank)
        {
            if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds) || deaths < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Run time and deaths must be finite and nonnegative.");
            string key = Key(stage, scenePath);
            Record previous = new Record(key);
            rank = Mathf.Clamp(rank, 1, 4);
            bool newTime = !previous.HasTime || seconds < previous.BestTime;
            bool cleanerTie = previous.HasTime && seconds == previous.BestTime && deaths < previous.Deaths;
            bool newRank = rank > previous.BestRank;
            if (newTime || cleanerTie)
            {
                PlayerPrefs.SetFloat(key + ".Time", seconds);
                PlayerPrefs.SetInt(key + ".TimeDeaths", deaths);
            }
            if (newRank) PlayerPrefs.SetInt(key + ".Rank", Mathf.Clamp(rank, 1, 4));
            PlayerPrefs.Save();
            return new SavedRun(previous.HasTime ? previous.BestTime : -1f, newTime, newRank);
        }

        public static void ClearBuildStage(int stage)
        {
            string key = Key(stage, BuildScenePath(stage));
            PlayerPrefs.DeleteKey(key + ".Time");
            PlayerPrefs.DeleteKey(key + ".TimeDeaths");
            PlayerPrefs.DeleteKey(key + ".Rank");
            PlayerPrefs.Save();
        }

        private static string SourcePath(int stage) => "Assets/_MagnetEscape/Scenes/Stage" + stage + ".unity";

        private static string Key(int stage, string scenePath)
        {
            if (stage < 1 || stage > 4) throw new ArgumentOutOfRangeException(nameof(stage));
            string key = "MagnetEscape.Stage" + stage;
            string path = string.IsNullOrEmpty(scenePath) ? SourcePath(stage) : scenePath.Replace('\\', '/');
            if (path == SourcePath(stage)) return key;
            // A deterministic identifier, unlike string.GetHashCode which can vary between runs.
            uint hash = 2166136261;
            foreach (char character in path) hash = unchecked((hash ^ character) * 16777619);
            return key + ".Course_" + hash.ToString("x8");
        }
    }
}
