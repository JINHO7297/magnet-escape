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
                case 1: return "도심 골목";
                case 2: return "옥상 질주";
                case 3: return "공중 레일";
                case 4: return "스카이라인 도전";
                default: return "마그넷 스케이터";
            }
        }

        public static string Lesson(int stage)
        {
            StageDefinition layout = Definition(stage);
            if (layout != null && !string.IsNullOrEmpty(layout.Lesson)) return layout.Lesson;
            switch (stage)
            {
                case 1: return "자동 전진과 점프 타이밍을 익히세요.";
                case 2: return "점프·대시·레일 도약으로 옥상을 건너세요.";
                case 3: return "공중 자력으로 비행 궤적을 조절하세요.";
                default: return "모든 기술을 조합해 결승에 도달하세요.";
            }
        }
    }
}
