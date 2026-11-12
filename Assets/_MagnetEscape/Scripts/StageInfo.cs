namespace MagnetEscape
{
    public static class StageInfo
    {
        public static string Title(int stage)
        {
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
