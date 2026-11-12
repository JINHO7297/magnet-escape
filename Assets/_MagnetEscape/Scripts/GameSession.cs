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
        private Vector2 spawn = new Vector2(0f, 1f);
        private Checkpoint currentCheckpoint;
        private bool respawning;
        private bool complete;
        private float elapsed;
        private int deaths;
        private AudioSource audioSource;
        private AudioClip tone;
        private Camera view;
        private string rank;

        private void Awake()
        {
            Active = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            // Small synthesized placeholder cue; replace with final sound design later.
            float[] samples = new float[4410];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Sin(i * 2f * Mathf.PI * 440f / 44100f) * (1f - (float)i / samples.Length) * 0.15f;
            tone = AudioClip.Create("Placeholder cue", samples.Length, 1, 44100, false);
            tone.SetData(samples, 0);
        }

        private void Start() => view = Camera.main;
        private void OnDestroy()
        {
            if (Active == this) Active = null;
            if (tone != null) Destroy(tone);
        }

        private void Update()
        {
            if (Stage == 0 || Player == null) return;
            if (!complete) elapsed += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene("StageSelect");
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene("Stage" + Stage);
            if (Player.transform.position.y < -7f) Die();
        }

        private void LateUpdate()
        {
            if (view == null || Player == null) return;
            Vector3 target = new Vector3(Player.transform.position.x + 4f, 3f, -10f);
            view.transform.position = Vector3.Lerp(view.transform.position, target, 1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        public void Die()
        {
            if (complete || respawning || Player == null) return;
            respawning = true;
            deaths++;
            Player.CanMove = false;
            Feedback(Player.transform.position, Color.red, 220f);
            StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(0.2f);
            Player.ResetAt(spawn);
            Player.CanMove = true;
            respawning = false;
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            if (complete || respawning || currentCheckpoint == checkpoint) return;
            currentCheckpoint = checkpoint;
            spawn = checkpoint.SpawnPoint;
            checkpoint.Activate();
            Feedback(spawn, Color.green, 880f);
        }

        public void Complete()
        {
            if (complete || respawning) return;
            complete = true;
            Player.CanMove = false;
            Player.ResetAt(Player.transform.position);
            rank = EvaluateRank(elapsed, deaths, Stage);
            string key = "MagnetEscape.Stage" + Stage;
            float best = PlayerPrefs.GetFloat(key + ".Time", float.MaxValue);
            if (elapsed < best) PlayerPrefs.SetFloat(key + ".Time", elapsed);
            int bestRank = PlayerPrefs.GetInt(key + ".Rank", 0);
            int score = rank == "S" ? 4 : rank == "A" ? 3 : rank == "B" ? 2 : 1;
            if (score > bestRank) PlayerPrefs.SetInt(key + ".Rank", score);
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
            audioSource.pitch = pitch / 440f;
            audioSource.PlayOneShot(tone);
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
            // Material is assigned explicitly for the built-in renderer.
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = StageBootstrap.ParticleMaterial;
            particles.Play();
            particles.Emit(12);
            Destroy(effect, 0.6f);
        }

        private void OnGUI()
        {
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            if (Stage == 0)
            {
                GUI.Box(new Rect(250, 50, 460, 430), "MAGNET ESCAPE");
                GUI.Label(new Rect(290, 85, 400, 30), "Escape the factory. Choose a stage.");
                string[] lessons = { "Move & jump", "Platforms, hazards & dash", "Attract & repel", "Final course" };
                for (int i = 1; i <= 4; i++)
                {
                    if (GUI.Button(new Rect(290, 120 + (i - 1) * 75, 380, 40), "Stage " + i + " - " + lessons[i - 1]))
                        SceneManager.LoadScene("Stage" + i);
                    string key = "MagnetEscape.Stage" + i;
                    if (PlayerPrefs.HasKey(key + ".Time"))
                        GUI.Label(new Rect(300, 160 + (i - 1) * 75, 380, 25), "Best time: " + PlayerPrefs.GetFloat(key + ".Time").ToString("F2") + "s | Best rank: " + " CBAS"[PlayerPrefs.GetInt(key + ".Rank", 1)]);
                }
                return;
            }
            GUI.Box(new Rect(10, 10, 940, 80), "Stage " + Stage + " | Time " + elapsed.ToString("F2") + "s | Deaths " + deaths);
            GUI.Label(new Rect(25, 36, 900, 25), "A/D or arrows: Move | Space: Jump (hold for height) | Shift: Dash | Q: Attract | E: Repel");
            GUI.Label(new Rect(25, 60, 900, 25), "R: Restart | Esc: Stage select | Dash: " + (Stage == 1 ? "Locked" : Player.Cooldown <= 0f ? "Ready" : Player.Cooldown.ToString("F1") + "s") + " | Magnet: " + (Stage < 3 ? "Locked" : Player.MagnetMode == 0 ? "Ready" : Player.MagnetMode > 0 ? "Attract" : "Repel"));
            if (respawning) GUI.Box(new Rect(360, 240, 240, 50), "Respawning...");
            if (!complete) return;
            GUI.Box(new Rect(280, 160, 400, 250), Stage == 4 ? "FACTORY ESCAPED!" : "STAGE CLEAR");
            GUI.Label(new Rect(310, 205, 350, 30), "Time: " + elapsed.ToString("F2") + "s | Deaths: " + deaths + " | Rank: " + rank);
            if (Stage < 4 && GUI.Button(new Rect(310, 250, 340, 35), "Next stage")) SceneManager.LoadScene("Stage" + (Stage + 1));
            if (GUI.Button(new Rect(310, 295, 340, 35), "Retry")) SceneManager.LoadScene("Stage" + Stage);
            if (GUI.Button(new Rect(310, 340, 340, 35), "Stage select")) SceneManager.LoadScene("StageSelect");
        }
    }
}
