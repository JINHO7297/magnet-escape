using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagnetEscape
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Active { get; private set; }
        public int Stage;
        public int RoutesBuilt { get; private set; }
        public bool IsEndless => Stage == StageInfo.EndlessStage;
        public float Distance { get; private set; }
        public int Stars { get; private set; }
        private int combo;
        public int Combo => Time.time - lastStar <= 2.5f ? combo : 0;
        public int BonusPoints { get; private set; }
        private float lastStar = -100f;
        public int EndlessScore => (int)System.Math.Min(int.MaxValue, (double)Distance * 10d + BonusPoints);
        private RouteShaper.Kind? lastFacility;
        private float lastFacilityTime;
        public int FacilitiesCompleted { get; private set; }
        public string FacilityFeedback { get; private set; }
        public bool ShowFacilityFeedback => FacilitiesCompleted > 0 && Elapsed - lastFacilityTime < 2.4f;
        public void RouteBuilt() => RoutesBuilt++;

        internal void CompleteFacility(RouteShaper.Kind kind, string label)
        {
            int points = kind == RouteShaper.Kind.Chain ? 320 : kind == RouteShaper.Kind.Convoy ? 260
                : kind == RouteShaper.Kind.Ceiling ? 220 : 180;
            bool linked = lastFacility.HasValue && lastFacility.Value != kind && Elapsed - lastFacilityTime <= 8f;
            FacilitiesCompleted++;
            lastFacility = kind;
            lastFacilityTime = Elapsed;
            if (IsEndless) BonusPoints += points + (linked ? 100 : 0);
            FacilityFeedback = label + (IsEndless ? $" +{points}" + (linked ? " · 기술 연속 +100" : "") : " 성공");
            Feedback(Player.transform.position, new Color(.65f, 1f, .8f), GameAudio.Cue.Checkpoint);
        }
        public void CollectStar()
        {
            if (!IsEndless || Paused || Finished || ChangingScene) return;
            combo = Time.time - lastStar <= 2.5f ? Mathf.Min(10, combo + 1) : 1;
            lastStar = Time.time; Stars++; BonusPoints += 50 + (Combo - 1) * 10;
        }
        public static bool HardEndless { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDifficulty() => HardEndless = false;

        public void StartEndless(bool hard)
        {
            HardEndless = hard;
            LoadEndless();
        }

        public void LoadEndless() => OpenScene("Endless");
        public PlayerMotor Player;
        public SpriteRenderer FactoryWall;
        public bool Paused { get; private set; }
        public bool Respawning { get; private set; }
        public bool Finished { get; private set; }
        public bool ChangingScene { get; private set; }
        public string CourseError { get; private set; }
        public float Elapsed { get; private set; }
        public int Deaths { get; private set; }
        public string Rank { get; private set; }
        public bool NewBestTime { get; private set; }
        public bool NewBestRank { get; private set; }
        public bool RecordSaved { get; private set; }
        public bool PracticeRun => string.IsNullOrEmpty(gameObject.scene.path);
        public float PreviousBestTime { get; private set; } = -1f;
        public string Hint { get; private set; }
        public bool HasCheckpoint => currentCheckpoint != null;
        public int CheckpointNumber => currentCheckpoint != null ? currentCheckpoint.Order : 0;
        public Camera ViewCamera => view;
        public Vector2 ExitPosition { get; private set; }
        public float Progress => Player != null ? Mathf.InverseLerp(startX, courseEnd, Player.transform.position.x) : 0f;
        public float RespawnProgress => Mathf.InverseLerp(startX, courseEnd, spawn.x);
        public string Notice => Time.unscaledTime < noticeUntil ? notice : string.Empty;
        [SerializeField] private float courseEnd;
        [SerializeField, Min(0f)] private float courseParTime;
        [SerializeField] private float fallDeathHeight = -7f;
        [SerializeField, Range(0f, 1f)] private float respawnDelay = 0.2f;
        public float CourseEnd { get => courseEnd; set => courseEnd = value; }
        public float RankTarget => courseParTime > 0f ? courseParTime : StageInfo.ParTime(Stage);
        // Best rank still available if the player clears without another death.
        // Use the same course snapshot and thresholds as the final result.
        public string RankPace => RankAtTarget(Elapsed, Deaths, RankTarget);
        public float RankTimeRemaining
        {
            get
            {
                string pace = RankPace;
                if (pace == "C") return 0f;
                return Mathf.Max(0f, RankTimeLimit(pace, RankTarget) - Elapsed);
            }
        }
        private Vector2 spawn = new Vector2(0f, 1f);
        private float startX;
        private float startY;
        private float cameraFloor;
        private float cameraCeiling;
        private bool geometryReady;
        private Checkpoint currentCheckpoint;
        private Camera view;
        private GameAudio audioFeedback;
        private GameHud hud;
        private string notice;
        private float noticeUntil;

        private void Awake()
        {
            Active = this;
            Time.timeScale = 1f;
            audioFeedback = gameObject.AddComponent<GameAudio>();
            hud = gameObject.AddComponent<GameHud>();
            AudioListener.volume = GameOptions.Volume;
            GameOptions.ApplyDisplayPreferences();
        }

        private void Start()
        {
            view = Camera.main;
            if (!string.IsNullOrEmpty(CourseError)) return;
            if (Stage > 0 && (Player == null || view == null || LayerMask.NameToLayer("Ground") < 0))
            {
                Debug.LogError("Course requires a player, Main Camera and Ground layer: " + gameObject.scene.path);
                ReportCourseError("코스 실행에 필요한 요소가 없습니다. 코스 선택으로 돌아가거나 다시 시도하세요.");
                return;
            }
            // Editable courses may place their robot somewhere other than (0, 1).
            if (Player != null) spawn = Player.GetComponent<Rigidbody2D>().position;
            startX = spawn.x;
            startY = spawn.y;
            ExitPosition = new Vector2(courseEnd, spawn.y);
            bool foundExit = false;
            foreach (ExitGate gate in FindObjectsOfType<ExitGate>())
            {
                if (gate.gameObject.scene != gameObject.scene) continue;
                ExitPosition = gate.transform.position;
                courseEnd = ExitPosition.x;
                foundExit = true;
                break;
            }
            if (Stage > 0 && !IsEndless && !foundExit)
            {
                Debug.LogError("Course has no ExitGate: " + gameObject.scene.path);
                ReportCourseError("결승이 없는 코스입니다. 코스 선택으로 돌아가세요.");
                return;
            }
            if (Player != null && !IsEndless) PrepareCourseGeometry();
            SnapCamera();
        }

        private void PrepareCourseGeometry()
        {
            Physics2D.SyncTransforms();
            Bounds bounds = new Bounds(new Vector3(startX, startY, 0f), Vector3.zero);
            bounds.Encapsulate(ExitPosition);
            int ground = LayerMask.NameToLayer("Ground");
            foreach (Collider2D collider in FindObjectsOfType<Collider2D>())
            {
                if (collider.gameObject.scene != gameObject.scene || collider.isTrigger || !collider.enabled || collider.gameObject.layer != ground) continue;
                Bounds platformBounds = collider.bounds;
                bounds.Encapsulate(platformBounds);
                MovingPlatform moving = collider.GetComponent<MovingPlatform>();
                if (moving == null) continue;
                platformBounds.center += (Vector3)moving.Travel;
                bounds.Encapsulate(platformBounds);
            }
            foreach (MagneticRail rail in MagneticRail.All)
            {
                if (rail.gameObject.scene != gameObject.scene) continue;
                bounds.Encapsulate(rail.StartPoint);
                bounds.Encapsulate(rail.EndPoint + Vector2.up);
            }
            foreach (MagnetAnchor anchor in MagnetAnchor.All)
            {
                if (anchor.gameObject.scene == gameObject.scene)
                    bounds.Encapsulate(new Bounds(anchor.transform.position, Vector3.one));
            }
            foreach (RotatingSaw saw in FindObjectsOfType<RotatingSaw>())
            {
                if (saw.gameObject.scene != gameObject.scene) continue;
                bounds.Encapsulate(new Bounds((Vector2)saw.transform.position - saw.Travel, Vector3.one));
                bounds.Encapsulate(new Bounds((Vector2)saw.transform.position + saw.Travel, Vector3.one));
            }
            foreach (Checkpoint checkpoint in FindObjectsOfType<Checkpoint>())
            {
                if (checkpoint.gameObject.scene != gameObject.scene) continue;
                bounds.Encapsulate(checkpoint.transform.position);
                bounds.Encapsulate(new Bounds(checkpoint.SpawnPoint, new Vector3(0.7f, 0.9f, 0f)));
            }
            cameraFloor = Mathf.Min(Mathf.Min(startY, ExitPosition.y) + 1.6f, bounds.min.y + 3.4f);
            cameraCeiling = Mathf.Max(Mathf.Max(startY, ExitPosition.y) + 5.8f, bounds.max.y + 1f);
            geometryReady = true;
            if (FactoryWall == null)
                foreach (SpriteRenderer renderer in FindObjectsOfType<SpriteRenderer>())
                    if (renderer.gameObject.scene == gameObject.scene && renderer.gameObject.name == GameArt.BackdropName)
                    {
                        FactoryWall = renderer;
                        break;
                    }
            if (FactoryWall != null) GameArt.FitBackdrop(FactoryWall, bounds);
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
                Time.timeScale = 1f;
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && Stage > 0 && !Finished) SetPaused(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) GameOptions.ToggleMute();
            if (Input.GetKeyDown(KeyCode.F11)) GameOptions.ToggleFullscreen();
            if (ChangingScene) return;
            if (Input.GetKeyDown(KeyCode.Escape) && hud.CloseOverlay()) return;
            if (hud.HasOverlay) return;
            if (!string.IsNullOrEmpty(CourseError))
            {
                if (Input.GetKeyDown(KeyCode.Escape)) ShowSelection();
                else if (Input.GetKeyDown(KeyCode.R)) LoadStage(Stage);
                return;
            }
            if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.C))
            {
                if (Stage > 0 && !Finished) SetPaused(true);
                if (Input.GetKeyDown(KeyCode.H)) hud.OpenHelp();
                else if (Input.GetKeyDown(KeyCode.T)) hud.OpenRecords();
                else hud.OpenCredits();
                return;
            }
            if (Stage == 0)
            {
                if (Input.GetKeyDown(KeyCode.N)) { StartEndless(false); return; }
                if (Input.GetKeyDown(KeyCode.B)) { hud.OpenRecords(); return; }
                for (int i = 1; i <= StageInfo.FestivalCount; i++)
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i))) { StartEndless(i == 2); return; }
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Finished) ShowSelection();
                else SetPaused(!Paused);
                return;
            }
            if (Player == null) return;
            if (Input.GetKeyDown(KeyCode.R)) { if (IsEndless) LoadEndless(); else LoadStage(Stage); return; }
            if (!Finished && Input.GetKeyDown(KeyCode.Backspace)) { RetryCheckpoint(); return; }
            if (Paused)
            {
                if (Input.GetKeyDown(KeyCode.Return)) SetPaused(false);
                else if (Input.GetKeyDown(KeyCode.Tab)) ShowSelection();
                return;
            }
            if (Finished)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (!IsEndless && StageInfo.NextStage(Stage) != 0) LoadStage(StageInfo.NextStage(Stage));
                    else ShowSelection();
                }
                return;
            }
            Elapsed += Time.deltaTime;
            if (IsEndless) Distance = Mathf.Max(Distance, Player.GetComponent<Rigidbody2D>().position.x);
            if (Player.transform.position.y < fallDeathHeight) Die();
            Hint = TutorialSign.NearestMessage(Player.transform.position);
        }

        private void LateUpdate()
        {
            if (view == null) return;
            if (Player != null && !Paused && !ChangingScene)
                view.transform.position = Vector3.Lerp(view.transform.position, CameraTarget(), 1f - Mathf.Exp(-7f * Time.deltaTime));
            if (FactoryWall != null) GameArt.FrameSky(FactoryWall, view, Stage == 0 || IsEndless ? .5f : Progress);
        }

        private Vector3 CameraTarget()
        {
            float direction = courseEnd >= startX ? 1f : -1f;
            if (IsEndless) return new Vector3(Player.transform.position.x + 4.5f, Mathf.Clamp(Player.transform.position.y + (Player.IsInverted ? -1f : 1.6f), 2f, 8f), -10f);
            float first = startX + direction * 3.5f;
            float last = courseEnd - direction * 3f;
            float x = Mathf.Abs(courseEnd - startX) < 6.5f ? (startX + courseEnd) * 0.5f
                : Mathf.Clamp(Player.transform.position.x + direction * 3.5f, Mathf.Min(first, last), Mathf.Max(first, last));
            float minY = geometryReady ? cameraFloor : Mathf.Min(startY, ExitPosition.y) + 1.6f;
            float maxY = geometryReady ? cameraCeiling : Mathf.Max(startY, ExitPosition.y) + 5.8f;
            float lookY = Player.IsInverted ? -1.5f : 1.5f;
            float y = Mathf.Clamp(Player.transform.position.y + lookY, minY, maxY);
            return new Vector3(x, y, -10f);
        }

        private void SnapCamera()
        {
            if (view != null && Player != null) view.transform.position = CameraTarget();
        }

        public void SetPaused(bool value)
        {
            if (Stage == 0 || Finished || ChangingScene || !string.IsNullOrEmpty(CourseError)) return;
            Paused = value;
            Time.timeScale = value ? 0f : 1f;
            Player?.ClearInput(preserveJumpRelease: true);
        }

        public void LoadStage(int stage)
        {
            if (stage == StageInfo.EndlessStage) { LoadEndless(); return; }
            OpenScene("Stage" + Mathf.Clamp(stage, 1, StageInfo.Count));
        }

        public bool CanOpenStage(int stage) => stage >= 1 && stage <= StageInfo.Count && Application.CanStreamedLevelBeLoaded("Stage" + stage);

        private void OpenScene(string name)
        {
            if (ChangingScene) return;
            if (!Application.CanStreamedLevelBeLoaded(name))
            {
                notice = "이 실행본에 포함되지 않은 코스입니다.";
                noticeUntil = Time.unscaledTime + 4f;
                Debug.LogError("Cannot open Magnet Escape scene: " + name);
                return;
            }
            float previousScale = Time.timeScale;
            ChangingScene = true;
            Time.timeScale = 1f;
            try { SceneManager.LoadScene(name); }
            catch (System.Exception exception)
            {
                ChangingScene = false;
                Time.timeScale = previousScale;
                notice = "코스를 불러오지 못했습니다. 다시 시도하세요.";
                noticeUntil = Time.unscaledTime + 4f;
                Debug.LogException(exception);
            }
        }

        public void ReportCourseError(string message)
        {
            CourseError = message;
            Paused = true;
            Time.timeScale = 0f;
            Player?.SetFrozen(true);
        }

        public void ConfigureCourse(float end, float deathHeight, float delay, float parTime = 0f)
        {
            courseEnd = end;
            fallDeathHeight = deathHeight;
            respawnDelay = Mathf.Clamp(delay, 0f, 1f);
            courseParTime = Mathf.Max(0f, parTime);
        }

        public void ShowSelection()
        {
            OpenScene("StageSelect");
        }

        public void QuitGame()
        {
            PlayerPrefs.Save();
            Time.timeScale = 1f;
            Application.Quit();
        }

        public void Die()
        {
            if (Finished || Respawning || Paused || ChangingScene || Player == null) return;
            if (IsEndless)
            {
                Distance = Mathf.Max(Distance, Player.GetComponent<Rigidbody2D>().position.x);
                Deaths++; Finished = true; Player.SetFrozen(true);
                Feedback(Player.transform.position, Color.red, GameAudio.Cue.Death);
                try
                {
                    EndlessRecords.Save(EndlessScore, Distance, Stars, GetComponent<EndlessCourse>().Seed, HardEndless);
                    RecordSaved = true;
                }
                catch (PlayerPrefsException exception) { Debug.LogException(exception); RecordSaved = false; }
                return;
            }
            Respawning = true;
            Deaths++;
            Player.SetFrozen(true);
            Feedback(Player.transform.position, Color.red, GameAudio.Cue.Death);
            StartCoroutine(Respawn());
        }

        public void RetryCheckpoint()
        {
            if (Finished || Respawning || ChangingScene || Player == null) return;
            if (Paused) SetPaused(false);
            Die();
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(Mathf.Clamp(respawnDelay, 0f, 1f));
            Player.ResetAt(spawn);
            Player.SetFrozen(false);
            Respawning = false;
            SnapCamera();
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            if (Finished || Respawning || ChangingScene || (currentCheckpoint != null && checkpoint.Order <= currentCheckpoint.Order)) return;
            currentCheckpoint = checkpoint;
            spawn = checkpoint.SpawnPoint;
            checkpoint.Activate();
            notice = "체크포인트 저장  /  " + checkpoint.Order;
            noticeUntil = Time.unscaledTime + 2.5f;
            Feedback(spawn, Color.green, GameAudio.Cue.Checkpoint);
        }

        public void Complete()
        {
            if (IsEndless) return;
            if (Finished || Respawning || Paused || ChangingScene || Player == null) return;
            Finished = true;
            Player.SetFrozen(true);
            Rank = RankAtTarget(Elapsed, Deaths, RankTarget);
            int score = Rank == "S" ? 4 : Rank == "A" ? 3 : Rank == "B" ? 2 : 1;
            try
            {
                StageRecords.SavedRun saved = StageRecords.Save(Stage, gameObject.scene.path, Elapsed, Deaths, score);
                PreviousBestTime = saved.PreviousTime;
                NewBestTime = saved.NewTime;
                NewBestRank = saved.NewRank;
                RecordSaved = saved.Persisted;
            }
            catch (PlayerPrefsException exception)
            {
                RecordSaved = false;
                Debug.LogException(exception);
            }
            Feedback(Player.transform.position, Color.yellow, GameAudio.Cue.Clear);
        }

        public static string EvaluateRank(float seconds, int deathCount, int stage)
        {
            return RankAtTarget(seconds, deathCount, StageInfo.ParTime(stage));
        }

        private static string RankAtTarget(float seconds, int deathCount, float target)
        {
            if (deathCount == 0 && seconds <= RankTimeLimit("S", target)) return "S";
            if (deathCount <= 2 && seconds <= RankTimeLimit("A", target)) return "A";
            if (deathCount <= 5 && seconds <= RankTimeLimit("B", target)) return "B";
            return "C";
        }

        private static float RankTimeLimit(string rank, float target)
        {
            return Mathf.Max(1f, target) * (rank == "S" ? 1f : rank == "A" ? 1.5f : 2f);
        }

        public void PlayCue(GameAudio.Cue cue) => audioFeedback.Play(cue);

        public void Feedback(Vector3 position, Color color, GameAudio.Cue cue)
        {
            PlayCue(cue);
            GameObject effect = new GameObject("Feedback");
            effect.transform.position = position;
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 2f;
            main.startSize = 0.13f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = StageBootstrap.ParticleMaterial;
            renderer.sortingOrder = 15;
            particles.Play();
            particles.Emit(12);
            Destroy(effect, 0.6f);
        }
    }
}
