using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    public static class EndlessRecords
    {
        public const string Key = "MagnetSkater.Endless.v3.Easy.Top5";
        private const string HardKey = "MagnetSkater.Endless.v3.Hard.Top5";
        [Serializable] public sealed class Entry
        {
            public int Score, Stars, Seed;
            public float Distance;
        }
        [Serializable] private sealed class Board { public List<Entry> Entries = new List<Entry>(); }
        public static List<Entry> Load(bool hard = false)
        {
            try
            {
                var board = JsonUtility.FromJson<Board>(PlayerPrefs.GetString(hard ? HardKey : Key, "{}"));
                var entries = board != null && board.Entries != null ? board.Entries : new List<Entry>();
                entries.RemoveAll(e => e == null || e.Score < 0 || e.Stars < 0 || e.Distance < 0 || float.IsNaN(e.Distance) || float.IsInfinity(e.Distance));
                entries.Sort((a,b) => b.Score.CompareTo(a.Score));
                if (entries.Count > 5) entries.RemoveRange(5, entries.Count - 5);
                return entries;
            }
            catch (ArgumentException) { return new List<Entry>(); }
        }
        public static void Save(int score, float distance, int stars, int seed, bool hard = false)
        {
            if (score < 0 || stars < 0 || distance < 0 || float.IsNaN(distance) || float.IsInfinity(distance)) throw new ArgumentOutOfRangeException(nameof(score));
            var entries = Load(hard); entries.Add(new Entry { Score = score, Distance = distance, Stars = stars, Seed = seed });
            entries.Sort((a,b) => b.Score.CompareTo(a.Score));
            if (entries.Count > 5) entries.RemoveRange(5, entries.Count - 5);
            PlayerPrefs.SetString(hard ? HardKey : Key, JsonUtility.ToJson(new Board { Entries = entries })); PlayerPrefs.Save();
        }
    }
}
