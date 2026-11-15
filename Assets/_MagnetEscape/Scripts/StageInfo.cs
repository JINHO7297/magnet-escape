using UnityEngine;

namespace MagnetEscape
{
    public static class StageInfo
    {
        private static readonly StageDefinition[] Definitions = new StageDefinition[5];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => System.Array.Clear(Definitions, 0, Definitions.Length);

        public static StageDefinition Definition(int stage)
        {
            if (stage < 1 || stage > 4) return null;
            if (Definitions[stage] == null) Definitions[stage] = Resources.Load<StageDefinition>("Stages/Stage" + stage);
            return Definitions[stage];
        }

        public static float ParTime(int stage)
        {
            StageDefinition layout = Definition(stage);
            return layout != null ? Mathf.Max(1f, layout.ParTime) : 25f + stage * 10f;
        }

        public static string Title(int stage)
        {
            StageDefinition layout = Definition(stage);
            if (layout != null && !string.IsNullOrEmpty(layout.DisplayName)) return layout.DisplayName;
            switch (stage)
            {
                case 1: return "ALLEY START";
                case 2: return "ROOFTOP RUSH";
                case 3: return "SKY RAIL";
                case 4: return "SKYLINE CHALLENGE";
                default: return "MAGNET SKATER";
            }
        }

        public static string Lesson(int stage)
        {
            StageDefinition layout = Definition(stage);
            if (layout != null && !string.IsNullOrEmpty(layout.Lesson)) return layout.Lesson;
            switch (stage)
            {
                case 1: return "Auto-run and time your jumps.";
                case 2: return "Jump and dash across rooftops.";
                case 3: return "Shape your flight with vertical magnetism.";
                default: return "Combine every skill. Reach the finish.";
            }
        }
    }
}
