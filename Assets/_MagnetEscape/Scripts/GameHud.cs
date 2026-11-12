using UnityEngine;

namespace MagnetEscape
{
    public sealed class GameHud : MonoBehaviour
    {
        private static readonly Color Mint = new Color(0.35f, 0.95f, 0.85f);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.35f);
        private GameSession session;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle text;
        private GUIStyle small;
        private GUIStyle button;
        private Texture2D normal;
        private Texture2D hover;
        private void Awake() => session = GetComponent<GameSession>();

        private void InitializeStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            foreach (GUIStyle style in new[] { title, heading, text, small }) style.normal.textColor = new Color(0.86f, 0.92f, 0.94f);
            normal = Solid(new Color(0.15f, 0.24f, 0.29f));
            hover = Solid(new Color(0.22f, 0.4f, 0.43f));
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            button.normal.background = normal;
            button.hover.background = hover;
            button.active.background = hover;
            button.normal.textColor = Color.white;
            button.hover.textColor = Mint;
            button.active.textColor = Gold;
        }

        private static Texture2D Solid(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void Label(Rect rect, string value, GUIStyle style, Color? color = null)
        {
            Color previous = GUI.contentColor;
            if (color.HasValue) GUI.contentColor = color.Value;
            GUI.Label(rect, value, style);
            GUI.contentColor = previous;
        }

        private void Panel(Rect rect)
        {
            Fill(rect, new Color(0.035f, 0.065f, 0.09f, 0.95f));
            Fill(new Rect(rect.x, rect.y, rect.width, 2), Mint);
        }

        private void OnGUI()
        {
            if (session == null) return;
            if (title == null) InitializeStyles();
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            Vector3 offset = new Vector3((Screen.width - 960f * scale) * 0.5f, (Screen.height - 540f * scale) * 0.5f, 0f);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            try
            {
                if (session.Stage == 0) Selection();
                else
                {
                    Hud();
                    if (session.Paused) Pause();
                    else if (session.Finished) Results();
                }
            }
            finally { GUI.matrix = previous; }
        }

        private void Selection()
        {
            Fill(new Rect(0, 0, 960, 540), new Color(0.02f, 0.04f, 0.06f, 0.72f));
            Label(new Rect(48, 24, 870, 55), "MAGNET ESCAPE", title, Mint);
            Label(new Rect(51, 82, 860, 30), "A small robot. An abandoned factory. Four routes to freedom.", text);
            int completed = 0;
            for (int stage = 1; stage <= 4; stage++)
                if (PlayerPrefs.HasKey("MagnetEscape.Stage" + stage + ".Time")) completed++;
            Label(new Rect(740, 45, 180, 30), "CLEARED  " + completed + " / 4", text, Gold);
            for (int stage = 1; stage <= 4; stage++)
            {
                float x = stage % 2 == 1 ? 48 : 494;
                float y = stage <= 2 ? 132 : 296;
                Panel(new Rect(x, y, 418, 146));
                Label(new Rect(x + 18, y + 14, 380, 30), "0" + stage + "  /  " + StageInfo.Title(stage), heading);
                Label(new Rect(x + 18, y + 49, 380, 22), StageInfo.Lesson(stage), small);
                string key = "MagnetEscape.Stage" + stage;
                string best = "NO RECORD YET";
                if (PlayerPrefs.HasKey(key + ".Time"))
                {
                    int score = Mathf.Clamp(PlayerPrefs.GetInt(key + ".Rank", 1), 1, 4);
                    best = "FASTEST " + PlayerPrefs.GetFloat(key + ".Time").ToString("F2") + "s / "
                        + PlayerPrefs.GetInt(key + ".TimeDeaths", 0) + " deaths | TOP RANK " + " CBAS"[score];
                }
                Label(new Rect(x + 18, y + 77, 380, 22), best, small, Gold);
                if (GUI.Button(new Rect(x + 18, y + 105, 382, 30), "START  /  " + stage, button)) session.LoadStage(stage);
            }
            Volume(52, 474);
            Label(new Rect(530, 476, 380, 35), "1 - 4: Select stage   |   M: Mute", small);
        }

        private void Hud()
        {
            Panel(new Rect(20, 16, 920, 66));
            Label(new Rect(36, 24, 420, 28), "0" + session.Stage + "  /  " + StageInfo.Title(session.Stage), heading);
            Label(new Rect(600, 24, 330, 28), session.Elapsed.ToString("F2") + "s     DEATHS " + session.Deaths, heading);
            PlayerMotor player = session.Player;
            if (player == null) return;
            Label(new Rect(36, 55, 200, 22), player.DashEnabled ? "DASH" : "DASH / LOCKED", small);
            Fill(new Rect(92, 61, 105, 5), new Color(0.2f, 0.27f, 0.3f));
            if (player.DashEnabled) Fill(new Rect(92, 61, 105 * player.DashReadyFraction, 5), Mint);
            string magnet = !player.MagnetEnabled ? "MAGNET / LOCKED" : player.MagnetTarget == null ? "MAGNET / OUT OF RANGE"
                : player.IsDashing ? "MAGNET / DASHING" : player.MagnetMode > 0 ? "Q / ATTRACTING"
                : player.MagnetMode < 0 ? "E / REPELLING" : "MAGNET / Q ATTRACT - E REPEL";
            Label(new Rect(240, 54, 530, 24), magnet, small, player.MagnetMode < 0 ? Gold : Mint);
            float progress = Mathf.Clamp01(player.transform.position.x / Mathf.Max(1f, session.CourseEnd));
            Fill(new Rect(20, 83, 920 * progress, 3), Mint);
            if (!string.IsNullOrEmpty(session.Notice))
            {
                Panel(new Rect(280, 104, 400, 38));
                Label(new Rect(296, 112, 370, 25), session.Notice, text, Gold);
            }
            if (!session.Paused && !session.Finished && !string.IsNullOrEmpty(session.Hint))
            {
                Panel(new Rect(120, 414, 720, 56));
                Label(new Rect(138, 425, 685, 44), session.Hint, text);
            }
            Fill(new Rect(20, 489, 920, 31), new Color(0.025f, 0.05f, 0.07f, 0.9f));
            Label(new Rect(34, 497, 895, 22), "A/D: Move   Space: Jump   WASD + Shift: Dash   Q/E: Magnet   Backspace: Retry   R: Restart   Esc: Pause", small);
            if (session.Respawning) Label(new Rect(370, 245, 250, 40), "REBOOTING...", heading, Gold);
        }

        private void Pause()
        {
            Fill(new Rect(0, 0, 960, 540), new Color(0f, 0f, 0f, 0.65f));
            Panel(new Rect(260, 90, 440, 365));
            Label(new Rect(292, 113, 380, 55), "PAUSED", title, Mint);
            if (GUI.Button(new Rect(292, 174, 376, 38), "RESUME  /  Esc", button)) session.SetPaused(false);
            if (GUI.Button(new Rect(292, 222, 376, 38), "RETRY CHECKPOINT  /  +1 DEATH", button)) session.RetryCheckpoint();
            if (GUI.Button(new Rect(292, 270, 376, 38), "RESTART STAGE", button)) session.LoadStage(session.Stage);
            if (GUI.Button(new Rect(292, 318, 376, 38), "STAGE SELECT", button)) session.ShowSelection();
            Volume(292, 385);
        }

        private void Results()
        {
            Fill(new Rect(0, 0, 960, 540), new Color(0f, 0f, 0f, 0.65f));
            Panel(new Rect(230, 87, 500, 375));
            Label(new Rect(263, 108, 450, 36), session.Stage == 4 ? "FACTORY ESCAPED" : "STAGE CLEAR", heading, Mint);
            Label(new Rect(267, 154, 410, 70), "RANK  " + session.Rank, title, Gold);
            Label(new Rect(270, 226, 420, 30), "TIME " + session.Elapsed.ToString("F2") + "s     /     DEATHS " + session.Deaths, text);
            string record = session.PreviousBestTime < 0f ? "FIRST CLEAR / NEW TIME RECORD"
                : (session.NewBestTime ? "NEW TIME RECORD / " : "PREVIOUS BEST / ")
                    + session.PreviousBestTime.ToString("F2") + "s  ("
                    + (session.Elapsed - session.PreviousBestTime).ToString("+0.00;-0.00;0.00") + "s)";
            Label(new Rect(270, 260, 420, 24), record, small, session.NewBestTime ? Gold : Mint);
            Label(new Rect(270, 286, 420, 22), session.NewBestRank ? "NEW BEST RANK  /  " + session.Rank
                : "S TARGET  /  no deaths and under " + (25 + session.Stage * 10) + "s", small);
            if (GUI.Button(new Rect(266, 308, 428, 40), session.Stage < 4 ? "NEXT STAGE  /  Enter" : "BACK TO STAGE SELECT  /  Enter", button))
            {
                if (session.Stage < 4) session.LoadStage(session.Stage + 1);
                else session.ShowSelection();
            }
            if (GUI.Button(new Rect(266, 360, 207, 40), "RETRY  /  R", button)) session.LoadStage(session.Stage);
            if (GUI.Button(new Rect(487, 360, 207, 40), "STAGE SELECT", button)) session.ShowSelection();
            Label(new Rect(270, 418, 425, 25), session.Stage == 4 ? "You made it outside. Every route is replayable." : "Your best time and best rank are saved separately.", small);
        }

        private void Volume(float x, float y)
        {
            Label(new Rect(x, y, 110, 25), "VOLUME", small);
            float volume = GUI.HorizontalSlider(new Rect(x + 85, y + 4, 180, 18), GameOptions.Volume, 0f, 1f);
            if (!Mathf.Approximately(volume, GameOptions.Volume)) GameOptions.SetVolume(volume);
            Label(new Rect(x + 278, y, 80, 25), Mathf.RoundToInt(volume * 100f) + "%", small);
            if (Event.current.type == EventType.MouseUp) PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (normal != null) Destroy(normal);
            if (hover != null) Destroy(hover);
        }
    }
}
