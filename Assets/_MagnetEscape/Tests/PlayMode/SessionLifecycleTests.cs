using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MagnetEscape.Tests
{
    // Exercise existing gameplay state without synthetic keyboard input or changing motor code.
    public sealed class SessionLifecycleTests
    {
        private const string Key = "MagnetEscape.Stage1";
        private bool hadTime;
        private bool hadDeaths;
        private bool hadRank;
        private float time;
        private int deaths;
        private int rank;
        private GameSession session;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            hadTime = PlayerPrefs.HasKey(Key + ".Time");
            hadDeaths = PlayerPrefs.HasKey(Key + ".TimeDeaths");
            hadRank = PlayerPrefs.HasKey(Key + ".Rank");
            time = PlayerPrefs.GetFloat(Key + ".Time");
            deaths = PlayerPrefs.GetInt(Key + ".TimeDeaths");
            rank = PlayerPrefs.GetInt(Key + ".Rank");
            Time.timeScale = 1f;
            SceneManager.LoadScene("Stage1");
            yield return null;
            session = GameSession.Active;
            Assert.That(session, Is.Not.Null);
            session.SetPaused(false);
            session.Player.enabled = false; // Tests do not depend on the user's physical keyboard.
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("StageSelect");
            yield return null;
            if (hadTime) PlayerPrefs.SetFloat(Key + ".Time", time);
            else PlayerPrefs.DeleteKey(Key + ".Time");
            if (hadDeaths) PlayerPrefs.SetInt(Key + ".TimeDeaths", deaths);
            else PlayerPrefs.DeleteKey(Key + ".TimeDeaths");
            if (hadRank) PlayerPrefs.SetInt(Key + ".Rank", rank);
            else PlayerPrefs.DeleteKey(Key + ".Rank");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator FourStagesLoadWithTheirAbilitiesAndRuntimeObjects()
        {
            for (int stage = 1; stage <= 4; stage++)
            {
                SceneManager.LoadScene("Stage" + stage);
                yield return null;
                GameSession active = GameSession.Active;
                Assert.That(active, Is.Not.Null);
                active.SetPaused(false);
                active.Player.enabled = false;
                Assert.That(active.Stage, Is.EqualTo(stage));
                Assert.That(active.Player.DashEnabled, Is.EqualTo(stage >= 2));
                Assert.That(active.Player.MagnetEnabled, Is.EqualTo(stage >= 3));
                Assert.That(active.CourseEnd, Is.GreaterThan(0f));
                Assert.That(Object.FindObjectsOfType<ExitGate>().Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsOfType<Checkpoint>().Length, Is.GreaterThan(0));
                Assert.That(Camera.main, Is.Not.Null);
                Assert.That(active.Player.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
            }
        }

        [UnityTest]
        public IEnumerator RepeatedDeathCountsOnceAndRestoresPhysics()
        {
            session.Die();
            session.Die();
            Assert.That(session.Deaths, Is.EqualTo(1));
            Assert.That(session.Player.GetComponent<Rigidbody2D>().simulated, Is.False);
            yield return WaitForRespawn();
            Assert.That(session.Deaths, Is.EqualTo(1));
            Assert.That(session.Player.CanMove, Is.True);
            Assert.That(session.Player.GetComponent<Rigidbody2D>().simulated, Is.True);
            Assert.That(session.Player.transform.position.x, Is.EqualTo(0f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator OlderCheckpointCannotReplaceTheLatestSpawn()
        {
            session.SetCheckpoint(CreateCheckpoint(new Vector2(2f, 2f), 200));
            session.SetCheckpoint(CreateCheckpoint(new Vector2(-1f, 2f), 100));
            session.Die();
            yield return WaitForRespawn();
            Assert.That(session.Player.transform.position.x, Is.EqualTo(2f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator PauseStopsTheRunClockAndPhysicsUntilResumed()
        {
            session.SetPaused(true);
            float stopped = session.Elapsed;
            Vector2 position = session.Player.GetComponent<Rigidbody2D>().position;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(session.Elapsed, Is.EqualTo(stopped));
            Assert.That(session.Player.GetComponent<Rigidbody2D>().position, Is.EqualTo(position));
            session.SetPaused(false);
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(session.Elapsed, Is.GreaterThan(stopped));
        }

        [UnityTest]
        public IEnumerator CompletionStopsTheClockAndRejectsFurtherDeaths()
        {
            session.Complete();
            float stopped = session.Elapsed;
            session.Complete();
            session.Die();
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(session.Finished, Is.True);
            Assert.That(session.Elapsed, Is.EqualTo(stopped));
            Assert.That(session.Deaths, Is.Zero);
            Assert.That(session.Player.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(session.Rank, Is.Not.Null.And.Not.Empty);
        }

        [UnityTest]
        public IEnumerator RestartCreatesANewUnpausedRunWithNoDeaths()
        {
            session.Die();
            yield return WaitForRespawn();
            session.SetPaused(true);
            session.LoadStage(1);
            yield return null;
            GameSession next = GameSession.Active;
            Assert.That(next, Is.Not.SameAs(session));
            Assert.That(next.Deaths, Is.Zero);
            Assert.That(next.Finished, Is.False);
            Assert.That(next.Respawning, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator ReturningToSelectionDoesNotKeepTheGamePaused()
        {
            session.SetPaused(true);
            session.ShowSelection();
            yield return null;
            Assert.That(GameSession.Active.Stage, Is.Zero);
            Assert.That(GameSession.Active.Player, Is.Null);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            GameSession.Active.LoadStage(4);
            yield return null;
            Assert.That(GameSession.Active.Stage, Is.EqualTo(4));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator ResetAtRemovesMomentumAndRestoresGravity()
        {
            Rigidbody2D body = session.Player.GetComponent<Rigidbody2D>();
            float configuredGravity = body.gravityScale;
            body.velocity = new Vector2(11f, -8f);
            body.angularVelocity = 20f;
            body.gravityScale = 0f;
            session.Player.ResetAt(new Vector2(1f, 3f));
            Assert.That(body.position, Is.EqualTo(new Vector2(1f, 3f)));
            Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
            Assert.That(body.angularVelocity, Is.Zero);
            Assert.That(body.gravityScale, Is.EqualTo(configuredGravity));
            Assert.That(session.Player.Cooldown, Is.Zero);
            yield break;
        }

        [UnityTest]
        public IEnumerator PlayerVisualReusesPrefabEffectsAndStopsThemWhenDisabled()
        {
            GameObject obj = new GameObject("Robot with prefab effects");
            obj.SetActive(false);
            obj.transform.position = new Vector2(0f, 50f);
            TrailRenderer trail = obj.AddComponent<TrailRenderer>();
            LineRenderer tether = obj.AddComponent<LineRenderer>();
            PlayerVisual visual = obj.AddComponent<PlayerVisual>();
            obj.SetActive(true);
            yield return null;
            Assert.That(obj.GetComponents<TrailRenderer>().Length, Is.EqualTo(1));
            Assert.That(obj.GetComponents<LineRenderer>().Length, Is.EqualTo(1));
            trail.emitting = true;
            tether.enabled = true;
            visual.enabled = false;
            Assert.That(trail.emitting, Is.False);
            Assert.That(tether.enabled, Is.False);
            Assert.That(obj.GetComponent<PlayerMotor>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ReconfiguredPlatformKeepsItsOriginalPatrolRail()
        {
            GameObject obj = new GameObject("Reconfigured platform");
            Vector2 origin = new Vector2(4f, 2f);
            obj.transform.position = origin;
            MovingPlatform platform = obj.AddComponent<MovingPlatform>();
            platform.Configure(Vector2.right * 3f, 4f);
            // Represent a platform part way through its patrol, not at its origin.
            obj.GetComponent<Rigidbody2D>().position = origin + Vector2.right;
            platform.Configure(Vector2.up * 3f, 2f);
            LineRenderer rail = obj.GetComponent<LineRenderer>();
            Assert.That((Vector2)rail.GetPosition(0), Is.EqualTo(origin));
            Assert.That((Vector2)rail.GetPosition(1), Is.EqualTo(origin + Vector2.up * 3f));
            Assert.That(obj.GetComponents<LineRenderer>().Length, Is.EqualTo(1));
            yield break;
        }

        [UnityTest]
        public IEnumerator RankPaceUsesCourseSnapshotAndMatchesTheClearResult()
        {
            session.ConfigureCourse(session.CourseEnd, -7f, 0f, 120f);
            Assert.That(session.RankPace, Is.EqualTo("S"));
            Assert.That(session.RankTimeRemaining, Is.EqualTo(120f - session.Elapsed).Within(0.001f));
            session.SetPaused(true);
            float stoppedBudget = session.RankTimeRemaining;
            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(session.RankPace, Is.EqualTo("S"));
            Assert.That(session.RankTimeRemaining, Is.EqualTo(stoppedBudget));
            session.SetPaused(false);
            for (int deaths = 1; deaths <= 6; deaths++)
            {
                session.Die();
                Assert.That(session.Deaths, Is.EqualTo(deaths));
                string expected = deaths <= 2 ? "A" : deaths <= 5 ? "B" : "C";
                Assert.That(session.RankPace, Is.EqualTo(expected));
                float budget = deaths <= 2 ? 180f - session.Elapsed : deaths <= 5 ? 240f - session.Elapsed : 0f;
                Assert.That(session.RankTimeRemaining, Is.EqualTo(budget).Within(0.001f));
                yield return WaitForRespawn();
            }
            session.Complete();
            Assert.That(session.Rank, Is.EqualTo(session.RankPace));
        }

        [UnityTest]
        public IEnumerator SawRouteUsesEditedOriginAndReusesItsRenderer()
        {
            session.SetPaused(true);
            GameObject obj = new GameObject("Edited saw route");
            obj.SetActive(false);
            obj.transform.position = new Vector2(9f, 5f);
            LineRenderer rail = obj.AddComponent<LineRenderer>();
            rail.positionCount = 2;
            rail.SetPosition(0, Vector3.zero); // Old serialized route before moving the saw.
            rail.SetPosition(1, Vector3.one);
            RotatingSaw saw = obj.AddComponent<RotatingSaw>();
            obj.SetActive(true);
            saw.Configure(new Vector2(3f, 1f), 4f, 0f);
            yield return null;
            Assert.That(obj.GetComponents<LineRenderer>().Length, Is.EqualTo(1));
            Assert.That((Vector2)rail.GetPosition(0), Is.EqualTo(new Vector2(6f, 4f)));
            Assert.That((Vector2)rail.GetPosition(1), Is.EqualTo(new Vector2(12f, 6f)));
            Assert.That(rail.enabled, Is.True);
            saw.Configure(Vector2.zero, 4f, 0f);
            Assert.That(rail.enabled, Is.False);
        }

        private static Checkpoint CreateCheckpoint(Vector2 position, int order)
        {
            GameObject obj = new GameObject("Test checkpoint");
            obj.transform.position = position;
            obj.AddComponent<SpriteRenderer>();
            Checkpoint checkpoint = obj.AddComponent<Checkpoint>();
            checkpoint.Order = order;
            return checkpoint;
        }

        private IEnumerator WaitForRespawn()
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (session.Respawning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.Respawning, Is.False, "Respawn did not finish within 3 real seconds.");
        }
    }
}
