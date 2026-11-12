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
        public bool Paused { get; private set; }
        public bool Respawning { get; private set; }
        public bool Finished { get; private set; }
        public float Elapsed { get; private set; }
        public int Deaths { get; private set; }
        public string Rank { get; private set; }
        public bool NewBestTime { get; private set; }
        public bool NewBestRank { get; private set; }
        public float PreviousBestTime { get; private set; } = -1f;
        public string Hint { get; private set; }
        public string Notice => Time.unscaledTime < noticeUntil ? notice : string.Empty;
        [SerializeField] private float courseEnd;
        public float CourseEnd { get => courseEnd; set => courseEnd = value; }
        private Vector2 spawn = new Vector2(0f, 1f);
        private Checkpoint currentCheckpoint;
        private Camera view;
        private GameAudio audioFeedback;
        private string notice;
        private float noticeUntil;

        private void Awake()
        {
            Active = this;
            Time.timeScale = 1f;
            audioFeedback = gameObject.AddComponent<GameAudio>();
            gameObject.AddComponent<GameHud>();
            AudioListener.volume = GameOptions.Volume;
        }

        private void Start()
        {
            view = Camera.main;
            SnapCamera();
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
            if (Stage == 0)
            {
                for (int i = 1; i <= 4; i++)
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i))) { LoadStage(i); return; }
                return;
            }
            if (Player == null) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Finished) ShowSelection();
                else SetPaused(!Paused);
                return;
            }
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
            if (Player.transform.position.y < -7f) Die();
            Hint = TutorialSign.NearestMessage(Player.transform.position);
        }

        private void LateUpdate()
        {
            if (view == null || Player == null || Paused) return;
            view.transform.position = Vector3.Lerp(view.transform.position, CameraTarget(), 1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        private Vector3 CameraTarget()
        {
            float x = Mathf.Clamp(Player.transform.position.x + 3.5f, 3.5f, Mathf.Max(3.5f, CourseEnd - 3f));
            float y = Mathf.Clamp(Player.transform.position.y + 1.5f, 2.6f, 7f);
            return new Vector3(x, y, -10f);
        }

        private void SnapCamera()
        {
            if (view != null && Player != null) view.transform.position = CameraTarget();
        }

        public void SetPaused(bool value)
        {
            if (Stage == 0 || Finished) return;
            Paused = value;
            Time.timeScale = value ? 0f : 1f;
            Player?.ClearInput();
        }

        public void LoadStage(int stage)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Stage" + Mathf.Clamp(stage, 1, 4));
        }

        public void ShowSelection()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("StageSelect");
        }

        public void Die()
        {
            if (Finished || Respawning || Paused || Player == null) return;
            Respawning = true;
            Deaths++;
            Player.SetFrozen(true);
            Feedback(Player.transform.position, Color.red, 220f);
            StartCoroutine(Respawn());
        }

        public void RetryCheckpoint()
        {
            if (Finished || Respawning || Player == null) return;
            if (Paused) SetPaused(false);
            Die();
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(0.2f);
            Player.ResetAt(spawn);
            Player.SetFrozen(false);
            Respawning = false;
            SnapCamera();
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            if (Finished || Respawning || (currentCheckpoint != null && checkpoint.Order <= currentCheckpoint.Order)) return;
            currentCheckpoint = checkpoint;
            spawn = checkpoint.SpawnPoint;
            checkpoint.Activate();
            notice = "CHECKPOINT SAVED  /  " + checkpoint.Order;
            noticeUntil = Time.unscaledTime + 2.5f;
            Feedback(spawn, Color.green, 880f);
        }

        public void Complete()
        {
            if (Finished || Respawning || Paused) return;
            Finished = true;
            Player.SetFrozen(true);
            Rank = EvaluateRank(Elapsed, Deaths, Stage);
            string key = "MagnetEscape.Stage" + Stage;
            PreviousBestTime = PlayerPrefs.HasKey(key + ".Time") ? PlayerPrefs.GetFloat(key + ".Time") : -1f;
            NewBestTime = PreviousBestTime < 0f || Elapsed < PreviousBestTime;
            if (NewBestTime)
            {
                PlayerPrefs.SetFloat(key + ".Time", Elapsed);
                PlayerPrefs.SetInt(key + ".TimeDeaths", Deaths);
            }
            int score = Rank == "S" ? 4 : Rank == "A" ? 3 : Rank == "B" ? 2 : 1;
            NewBestRank = score > PlayerPrefs.GetInt(key + ".Rank", 0);
            if (NewBestRank) PlayerPrefs.SetInt(key + ".Rank", score);
            PlayerPrefs.Save();
            Feedback(Player.transform.position, Color.yellow, 1100f);
        }

        public static string EvaluateRank(float seconds, int deathCount, int stage)
        {
            float par = 25f + stage * 10f;
            if (deathCount == 0 && seconds <= par) return "S";
            if (deathCount <= 2 && seconds <= par * 1.5f) return "A";
            if (deathCount <= 5 && seconds <= par * 2f) return "B";
            return "C";
        }

        public void Feedback(Vector3 position, Color color, float pitch)
        {
            audioFeedback.Play(pitch);
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
