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
                case 1: return "ASSEMBLY LINE";
                case 2: return "MACHINE HALL";
                case 3: return "MAGNETIC SHAFT";
                case 4: return "LAST EXIT";
                default: return "MAGNET ESCAPE";
            }
        }

        public static string Lesson(int stage)
        {
            StageDefinition layout = Definition(stage);
            if (layout != null && !string.IsNullOrEmpty(layout.Lesson)) return layout.Lesson;
            switch (stage)
            {
                case 1: return "Learn to move, jump and land.";
                case 2: return "Ride the machinery. Dash past danger.";
                case 3: return "Attract, release, then repel.";
                default: return "Combine every skill. Leave the factory.";
            }
        }
    }
}
