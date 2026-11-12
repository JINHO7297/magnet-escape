using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagnetEscape
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Active { get; private set; }
        public int Stage;
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
                ReportCourseError("This course is missing something needed to play. Return to stage selection or try again.");
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
            if (Stage > 0 && !foundExit)
            {
                Debug.LogError("Course has no ExitGate: " + gameObject.scene.path);
                ReportCourseError("This course has no available exit. Return to stage selection.");
                return;
            }
            if (Player != null) PrepareCourseGeometry();
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
            if (ChangingScene) return;
            if (Input.GetKeyDown(KeyCode.Escape) && hud.CloseOverlay()) return;
            if (hud.HasOverlay) return;
            if (!string.IsNullOrEmpty(CourseError))
            {
                if (Input.GetKeyDown(KeyCode.Escape)) ShowSelection();
                else if (Input.GetKeyDown(KeyCode.R)) LoadStage(Stage);
                return;
            }
            if (Stage == 0)
            {
                for (int i = 1; i <= 4; i++)
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i))) { LoadStage(i); return; }
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Finished) ShowSelection();
                else SetPaused(!Paused);
                return;
            }
            if (Player == null) return;
            if (Paused) return;
            if (Input.GetKeyDown(KeyCode.R)) { LoadStage(Stage); return; }
            if (!Finished && Input.GetKeyDown(KeyCode.Backspace)) { RetryCheckpoint(); return; }
            if (Finished)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (Stage < 4) LoadStage(Stage + 1);
                    else ShowSelection();
                }
                return;
            }
            Elapsed += Time.deltaTime;
            if (Player.transform.position.y < fallDeathHeight) Die();
            Hint = TutorialSign.NearestMessage(Player.transform.position);
        }

        private void LateUpdate()
        {
            if (view == null || Player == null || Paused || ChangingScene) return;
            view.transform.position = Vector3.Lerp(view.transform.position, CameraTarget(), 1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        private Vector3 CameraTarget()
        {
            float direction = courseEnd >= startX ? 1f : -1f;
            float first = startX + direction * 3.5f;
            float last = courseEnd - direction * 3f;
            float x = Mathf.Abs(courseEnd - startX) < 6.5f ? (startX + courseEnd) * 0.5f
                : Mathf.Clamp(Player.transform.position.x + direction * 3.5f, Mathf.Min(first, last), Mathf.Max(first, last));
            float minY = geometryReady ? cameraFloor : Mathf.Min(startY, ExitPosition.y) + 1.6f;
            float maxY = geometryReady ? cameraCeiling : Mathf.Max(startY, ExitPosition.y) + 5.8f;
            float y = Mathf.Clamp(Player.transform.position.y + 1.5f, minY, maxY);
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
            Player?.ClearInput();
        }

        public void LoadStage(int stage)
        {
            OpenScene("Stage" + Mathf.Clamp(stage, 1, 4));
        }

        public bool CanOpenStage(int stage) => stage >= 1 && stage <= 4 && Application.CanStreamedLevelBeLoaded("Stage" + stage);

        private void OpenScene(string name)
        {
            if (ChangingScene) return;
            if (!Application.CanStreamedLevelBeLoaded(name))
            {
                notice = "This course is not available in this build.";
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
                notice = "Could not open the course. Please try again.";
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
            notice = "CHECKPOINT SAVED  /  " + checkpoint.Order;
            noticeUntil = Time.unscaledTime + 2.5f;
            Feedback(spawn, Color.green, GameAudio.Cue.Checkpoint);
        }

        public void Complete()
        {
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
            float par = Mathf.Max(1f, target);
            if (deathCount == 0 && seconds <= par) return "S";
            if (deathCount <= 2 && seconds <= par * 1.5f) return "A";
            if (deathCount <= 5 && seconds <= par * 2f) return "B";
            return "C";
        }

        public void Feedback(Vector3 position, Color color, GameAudio.Cue cue)
        {
            audioFeedback.Play(cue);
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
