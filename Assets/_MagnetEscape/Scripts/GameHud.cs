using UnityEngine;

namespace MagnetEscape
{
    public sealed class GameHud : MonoBehaviour
    {
        private enum Overlay { None, Controls, Credits, Records, ResetRecord }
        private Overlay overlay;
        private int resetStage;
        private string resetScenePath;
        private string resetTitle;
        private string recordWarning;
        public bool HasOverlay => overlay != Overlay.None;
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
        private float uiScale;
        private Vector2 uiOffset;
        private void Awake() => session = GetComponent<GameSession>();

        private void OpenRecords()
        {
            recordWarning = null;
            overlay = Overlay.Records;
        }

        public bool CloseOverlay()
        {
            if (!HasOverlay) return false;
            overlay = overlay == Overlay.ResetRecord ? Overlay.Records : Overlay.None;
            return true;
        }

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
            if (session == null || Screen.width <= 0 || Screen.height <= 0) return;
            if (title == null) InitializeStyles();
            Matrix4x4 previous = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, Screen.width, Screen.height);
            float scale = Mathf.Min(safe.width / 960f, safe.height / 540f);
            Vector3 offset = new Vector3(safe.xMin + (safe.width - 960f * scale) * 0.5f,
                Screen.height - safe.yMax + (safe.height - 540f * scale) * 0.5f, 0f);
            uiScale = scale;
            uiOffset = offset;
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            try
            {
                GUI.enabled = previousEnabled && !HasOverlay && !session.ChangingScene;
                if (session.Stage == 0) Selection();
                else if (!string.IsNullOrEmpty(session.CourseError)) CourseFailure();
                else
                {
                    GUI.enabled = previousEnabled && !HasOverlay && !session.ChangingScene && !session.Paused && !session.Finished;
                    Hud();
                    GUI.enabled = previousEnabled && !HasOverlay && !session.ChangingScene;
                    if (session.Paused) Pause();
                    else if (session.Finished) Results();
                }
                GUI.enabled = previousEnabled && !session.ChangingScene;
                if (HasOverlay) InformationOverlay();
                if (!HasOverlay && session.Stage > 0 && (session.Paused || session.Finished)
                    && string.IsNullOrEmpty(session.CourseError) && !string.IsNullOrEmpty(session.Notice))
                {
                    Panel(new Rect(170, 18, 620, 50));
                    Label(new Rect(188, 30, 584, 32), session.Notice, text, Gold);
                }
                if (session.ChangingScene)
                {
                    Fill(new Rect(0, 0, 960, 540), new Color(0f, 0f, 0f, 0.7f));
                    Panel(new Rect(300, 225, 360, 80));
                    Label(new Rect(328, 250, 320, 35), "OPENING COURSE...", heading, Mint);
                }
            }
            finally { GUI.matrix = previous; GUI.enabled = previousEnabled; }
        }

        private void Selection()
        {
            Fill(new Rect(0, 0, 960, 540), new Color(0.02f, 0.04f, 0.06f, 0.72f));
            Label(new Rect(48, 24, 870, 55), "MAGNET ESCAPE", title, Mint);
            Label(new Rect(51, 82, 860, 30), "A small robot. An abandoned factory. Four routes to freedom.", text);
            if (!string.IsNullOrEmpty(session.Notice)) Label(new Rect(52, 112, 860, 20), session.Notice, small, Gold);
            int completed = 0;
            for (int stage = 1; stage <= 4; stage++)
                if (StageRecords.ForBuildStage(stage).HasTime) completed++;
            Label(new Rect(740, 45, 180, 30), "CLEARED  " + completed + " / 4", text, Gold);
            for (int stage = 1; stage <= 4; stage++)
            {
                float x = stage % 2 == 1 ? 48 : 494;
                float y = stage <= 2 ? 132 : 296;
                Panel(new Rect(x, y, 418, 146));
                Label(new Rect(x + 18, y + 14, 380, 30), "0" + stage + "  /  " + StageInfo.Title(stage), heading);
                Label(new Rect(x + 18, y + 49, 380, 22), StageInfo.Lesson(stage), small);
                StageRecords.Record record = StageRecords.ForBuildStage(stage);
                bool available = session.CanOpenStage(stage);
                string best = available ? "NO RECORD YET" : "COURSE NOT IN THIS BUILD";
                if (available && record.HasTime)
                {
                    int score = Mathf.Clamp(record.BestRank, 1, 4);
                    best = "FASTEST " + record.BestTime.ToString("F2") + "s / "
                        + record.Deaths + " deaths | TOP RANK " + " CBAS"[score];
                }
                Label(new Rect(x + 18, y + 77, 380, 22), best, small, Gold);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && available;
                if (GUI.Button(new Rect(x + 18, y + 105, 382, 30), available ? "START  /  " + stage : "UNAVAILABLE", button)) session.LoadStage(stage);
                GUI.enabled = enabled;
            }
            Volume(52, 474);
            if (GUI.Button(new Rect(450, 474, 140, 32), "RECORDS", button)) OpenRecords();
            if (GUI.Button(new Rect(610, 474, 90, 32), "HELP", button)) overlay = Overlay.Controls;
            if (GUI.Button(new Rect(710, 474, 90, 32), "CREDITS", button)) overlay = Overlay.Credits;
            if (!Application.isEditor && GUI.Button(new Rect(810, 474, 100, 32), "QUIT", button)) session.QuitGame();
            DisplayModeButton(new Rect(52, 510, 230, 26));
            Label(new Rect(530, 516, 380, 20), "1 - 4: Select stage   |   M: Mute", small);
        }

        private void Hud()
        {
            Panel(new Rect(20, 16, 920, 66));
            Label(new Rect(36, 24, 420, 28), "0" + session.Stage + "  /  " + StageInfo.Title(session.Stage), heading);
            Label(new Rect(600, 24, 330, 28), session.Elapsed.ToString("F2") + "s     DEATHS " + session.Deaths, heading);
            if (GUI.Button(new Rect(480, 26, 90, 28), "HELP", button))
            {
                session.SetPaused(true);
                overlay = Overlay.Controls;
            }
            PlayerMotor player = session.Player;
            if (player == null) return;
            Label(new Rect(36, 55, 200, 22), player.DashEnabled ? "DASH" : "DASH / LOCKED", small);
            Fill(new Rect(92, 61, 105, 5), new Color(0.2f, 0.27f, 0.3f));
            if (player.DashEnabled) Fill(new Rect(92, 61, 105 * player.DashReadyFraction, 5), Mint);
            string magnet = !player.MagnetEnabled ? "MAGNET / LOCKED" : player.MagnetTarget == null ? "MAGNET / OUT OF RANGE"
                : player.IsDashing ? "MAGNET / DASHING" : player.MagnetMode > 0 ? "Q / ATTRACTING"
                : player.MagnetMode < 0 ? "E / REPELLING" : "MAGNET / Q ATTRACT - E REPEL";
            Label(new Rect(240, 54, 530, 24), magnet, small, player.MagnetMode < 0 ? Gold : Mint);
            Label(new Rect(800, 54, 125, 24), session.HasCheckpoint ? "CP SAVED / " + session.CheckpointNumber : "NO CP YET", small, Gold);
            Fill(new Rect(20, 83, 920 * session.Progress, 3), Mint);
            if (session.HasCheckpoint) Fill(new Rect(18 + 920 * session.RespawnProgress, 80, 4, 10), Gold);
            ExitIndicator();
            if (!session.Paused && !session.Finished)
            {
                Panel(new Rect(20, 100, 250, 42));
                string pace = session.RankPace;
                string budget = pace == "C" ? "FINISH TO CLEAR" : session.RankTimeRemaining.ToString("F1") + "s LEFT";
                Label(new Rect(32, 113, 230, 24), "RANK PACE " + pace + "  /  " + budget, small, pace == "S" ? Gold : Mint);
            }
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

        private void CourseFailure()
        {
            Fill(new Rect(0, 0, 960, 540), new Color(0f, 0f, 0f, 0.7f));
            Panel(new Rect(170, 135, 620, 270));
            Label(new Rect(200, 160, 560, 40), "COURSE UNAVAILABLE", heading, Gold);
            Label(new Rect(200, 218, 560, 70), session.CourseError, text);
            if (!string.IsNullOrEmpty(session.Notice)) Label(new Rect(200, 290, 560, 32), session.Notice, small, Gold);
            if (GUI.Button(new Rect(200, 337, 265, 40), "STAGE SELECT  /  Esc", button)) session.ShowSelection();
            if (GUI.Button(new Rect(485, 337, 275, 40), "TRY AGAIN  /  R", button)) session.LoadStage(session.Stage);
        }

        private void Pause()
        {
            Fill(new Rect(0, 0, 960, 540), new Color(0f, 0f, 0f, 0.65f));
            Panel(new Rect(260, 90, 440, 400));
            Label(new Rect(292, 113, 380, 55), "PAUSED", title, Mint);
            if (GUI.Button(new Rect(292, 174, 376, 38), "RESUME  /  Esc", button)) session.SetPaused(false);
            if (GUI.Button(new Rect(292, 222, 376, 38), "RETRY CHECKPOINT  /  +1 DEATH", button)) session.RetryCheckpoint();
            if (GUI.Button(new Rect(292, 270, 376, 38), "RESTART STAGE", button)) session.LoadStage(session.Stage);
            if (GUI.Button(new Rect(292, 318, 376, 38), "STAGE SELECT", button)) session.ShowSelection();
            if (GUI.Button(new Rect(292, 365, 118, 30), "HELP", button)) overlay = Overlay.Controls;
            if (GUI.Button(new Rect(420, 365, 118, 30), "RECORDS", button)) OpenRecords();
            if (GUI.Button(new Rect(548, 365, 120, 30), "CREDITS", button)) overlay = Overlay.Credits;
            Volume(292, 414);
            DisplayModeButton(new Rect(292, 448, 376, 30));
        }

        private void DisplayModeButton(Rect rect)
        {
            if (Application.isEditor) return;
            if (GUI.Button(rect, Screen.fullScreen ? "SWITCH TO WINDOWED" : "SWITCH TO FULLSCREEN", button))
                GameOptions.ToggleFullscreen();
        }

        private void ExitIndicator()
        {
            if (session.Paused || session.Finished || session.Respawning || session.ViewCamera == null) return;
            Vector3 viewport = session.ViewCamera.WorldToViewportPoint(session.ExitPosition);
            if (viewport.z <= 0f || (viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f)) return;
            Vector3 screen = session.ViewCamera.WorldToScreenPoint(session.ExitPosition);
            float x = Mathf.Clamp((screen.x - uiOffset.x) / uiScale, 70f, 890f);
            float y = Mathf.Clamp((Screen.height - screen.y - uiOffset.y) / uiScale, 166f, 392f);
            string direction = viewport.x > 1f ? "EXIT >" : viewport.x < 0f ? "< EXIT" : viewport.y > 1f ? "EXIT ^" : "EXIT v";
            Panel(new Rect(x - 44f, y - 16f, 88f, 32f));
            Label(new Rect(x - 32f, y - 8f, 78f, 24f), direction, small, Mint);
        }

        private void Results()
        {
            Fill(new Rect(0, 0, 960, 540), new Color(0f, 0f, 0f, 0.65f));
            Panel(new Rect(230, 87, 500, 375));
            Label(new Rect(263, 108, 450, 36), session.Stage == 4 ? "FACTORY ESCAPED" : "STAGE CLEAR", heading, Mint);
            Label(new Rect(267, 154, 410, 70), "RANK  " + session.Rank, title, Gold);
            Label(new Rect(270, 226, 420, 30), "TIME " + session.Elapsed.ToString("F2") + "s     /     DEATHS " + session.Deaths, text);
            string record = !session.RecordSaved ? (session.PracticeRun ? "PRACTICE RUN / RECORD NOT SAVED" : "RECORD COULD NOT BE SAVED")
                : session.PreviousBestTime < 0f ? "FIRST CLEAR / NEW TIME RECORD"
                : (session.NewBestTime ? "NEW TIME RECORD / " : "PREVIOUS BEST / ")
                    + session.PreviousBestTime.ToString("F2") + "s  ("
                    + (session.Elapsed - session.PreviousBestTime).ToString("+0.00;-0.00;0.00") + "s)";
            Label(new Rect(270, 260, 420, 24), record, small, session.NewBestTime ? Gold : Mint);
            Label(new Rect(270, 286, 420, 22), session.NewBestRank ? "NEW BEST RANK  /  " + session.Rank
                : "S TARGET  /  no deaths and under " + session.RankTarget.ToString("F1") + "s", small);
            bool nextAvailable = session.Stage >= 4 || session.CanOpenStage(session.Stage + 1);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && nextAvailable;
            string nextLabel = session.Stage >= 4 ? "BACK TO STAGE SELECT  /  Enter"
                : nextAvailable ? "NEXT STAGE  /  Enter" : "NEXT COURSE NOT IN THIS BUILD";
            if (GUI.Button(new Rect(266, 308, 428, 40), nextLabel, button))
            {
                if (session.Stage < 4) session.LoadStage(session.Stage + 1);
                else session.ShowSelection();
            }
            GUI.enabled = enabled;
            if (GUI.Button(new Rect(266, 360, 207, 40), "RETRY  /  R", button)) session.LoadStage(session.Stage);
            if (GUI.Button(new Rect(487, 360, 207, 40), "STAGE SELECT", button)) session.ShowSelection();
            Label(new Rect(270, 418, 425, 25), !session.RecordSaved ? (session.PracticeRun ? "This run does not affect your saved records." : "The stage is clear, but its record was not saved.")
                : session.Stage == 4 ? "You made it outside. Every route is replayable." : "Your best time and best rank are saved separately.", small);
        }

        private void Volume(float x, float y)
        {
            Label(new Rect(x, y, 110, 25), "VOLUME", small);
            float volume = GUI.HorizontalSlider(new Rect(x + 85, y + 4, 180, 18), GameOptions.Volume, 0f, 1f);
            if (!Mathf.Approximately(volume, GameOptions.Volume)) GameOptions.SetVolume(volume);
            Label(new Rect(x + 278, y, 80, 25), Mathf.RoundToInt(volume * 100f) + "%", small);
            if (Event.current.type == EventType.MouseUp) PlayerPrefs.Save();
        }

        private void InformationOverlay()
        {
            Fill(new Rect(0, 0, 960, 540), new Color(0f, 0f, 0f, 0.8f));
            Panel(new Rect(100, 40, 760, 460));
            string caption = overlay == Overlay.Controls ? "HOW TO ESCAPE" : overlay == Overlay.Records ? "COURSE RECORDS"
                : overlay == Overlay.ResetRecord ? "CLEAR THIS RECORD?" : "MAGNET ESCAPE";
            Label(new Rect(130, 64, 700, 55), caption, title, Mint);
            if (overlay == Overlay.Controls)
            {
                Label(new Rect(132, 125, 690, 25), "Reach the green exit. New abilities are introduced one stage at a time.", small);
                string[] keys = { "A / D or arrows", "SPACE", "WASD + SHIFT", "Q / E", "BACKSPACE", "R", "ESC" };
                string[] actions = {
                    "Move left and right.", "Hold for height. Release early for a short jump.",
                    "Dash in the held direction. Watch the cooldown bar.",
                    "Attract / repel. Release to coast after a magnetic launch.",
                    "Retry your checkpoint. Adds one death; keeps the timer.",
                    "Restart the stage with a new timer and death count.", "Pause / resume. M toggles sound."
                };
                for (int i = 0; i < keys.Length; i++)
                {
                    Label(new Rect(132, 160 + i * 37, 210, 30), keys[i], text, Gold);
                    Label(new Rect(350, 160 + i * 37, 475, 34), actions[i], small);
                }
                Label(new Rect(132, 423, 690, 22), "Gold rails: platforms. Red rails: saws. Cyan circles: magnetic range.", small);
            }
            else if (overlay == Overlay.Records)
            {
                Label(new Rect(132, 125, 690, 25), "Records for the four courses currently selected for this build.", small);
                for (int stage = 1; stage <= 4; stage++)
                {
                    float y = 158 + (stage - 1) * 66;
                    string scenePath = StageRecords.BuildScenePath(stage);
                    StageRecords.Record record = StageRecords.ForScene(stage, scenePath);
                    bool available = !string.IsNullOrEmpty(scenePath) && session.CanOpenStage(stage);
                    Label(new Rect(132, y, 555, 28), "0" + stage + "  /  " + StageInfo.Title(stage), heading);
                    string value = !available ? "COURSE NOT IN THIS BUILD" : record.HasTime ? record.BestTime.ToString("F2") + "s / " + record.Deaths + " deaths"
                        + " / Top rank " + " CBAS"[Mathf.Clamp(record.BestRank, 1, 4)] : "NO RECORD YET";
                    Label(new Rect(132, y + 30, 555, 24), value, small, Gold);
                    bool enabled = GUI.enabled;
                    GUI.enabled = enabled && available && (record.HasTime || record.BestRank > 0);
                    if (GUI.Button(new Rect(718, y + 10, 104, 32), "CLEAR", button))
                    {
                        resetStage = stage;
                        resetScenePath = scenePath;
                        resetTitle = StageInfo.Title(stage);
                        overlay = Overlay.ResetRecord;
                    }
                    GUI.enabled = enabled;
                }
                if (!string.IsNullOrEmpty(recordWarning)) Label(new Rect(132, 421, 690, 25), recordWarning, small, Gold);
            }
            else if (overlay == Overlay.ResetRecord)
            {
                Label(new Rect(132, 148, 690, 35), "STAGE " + resetStage + "  /  " + resetTitle, heading, Gold);
                Label(new Rect(132, 210, 690, 100), "Clear the fastest time, deaths on that run and top rank for this course? "
                    + "This cannot be undone. Other courses and sound settings stay as they are. Your current run is not restarted.", text);
                if (GUI.Button(new Rect(160, 352, 300, 44), "CLEAR THIS COURSE", button))
                {
                    try
                    {
                        recordWarning = StageRecords.ClearCourse(resetStage, resetScenePath) ? null : "This course record is not available.";
                    }
                    catch (PlayerPrefsException exception)
                    {
                        recordWarning = "The record change could not be saved on this device.";
                        Debug.LogException(exception);
                    }
                    overlay = Overlay.Records;
                }
                if (GUI.Button(new Rect(490, 352, 300, 44), "CANCEL  /  Esc", button)) overlay = Overlay.Records;
            }
            else
            {
                Label(new Rect(132, 130, 680, 55), "A small robot escapes an abandoned factory through four precision platforming courses.", text);
                Label(new Rect(132, 213, 680, 30), "PROJECT  /  JINHO7297", heading, Gold);
                Label(new Rect(132, 263, 680, 30), "ART  /  Original robot and factory pixel art", text);
                Label(new Rect(132, 306, 680, 30), "SOUND  /  Original synthesized effects", text);
                Label(new Rect(132, 349, 680, 30), "BUILT WITH  /  Unity", text);
                Label(new Rect(132, 401, 680, 30), "Thank you for playing. Every course is replayable.", text, Mint);
            }
            if (overlay != Overlay.ResetRecord && GUI.Button(new Rect(330, 454, 300, 32), "BACK  /  Esc", button)) overlay = Overlay.None;
        }

        private void OnDestroy()
        {
            if (normal != null) Destroy(normal);
            if (hover != null) Destroy(hover);
        }
    }
}
