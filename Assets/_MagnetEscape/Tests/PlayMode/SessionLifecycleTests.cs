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
        private readonly System.Collections.Generic.Dictionary<string, float> savedTimes = new System.Collections.Generic.Dictionary<string, float>();
        private readonly System.Collections.Generic.Dictionary<string, int> savedInts = new System.Collections.Generic.Dictionary<string, int>();
        private GameSession session;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            savedTimes.Clear();
            savedInts.Clear();
            for (int stage = 1; stage <= 4; stage++)
            {
                string key = "MagnetSkater.Stage" + stage;
                if (PlayerPrefs.HasKey(key + ".Time")) savedTimes[key + ".Time"] = PlayerPrefs.GetFloat(key + ".Time");
                foreach (string suffix in new[] { ".TimeDeaths", ".Rank" })
                    if (PlayerPrefs.HasKey(key + suffix)) savedInts[key + suffix] = PlayerPrefs.GetInt(key + suffix);
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene("Stage1");
            yield return null;
            yield return null; // Let GameSession.Start snapshot the initial spawn before test commands.
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
            for (int stage = 1; stage <= 4; stage++)
            {
                string key = "MagnetSkater.Stage" + stage;
                foreach (string suffix in new[] { ".Time", ".TimeDeaths", ".Rank" }) PlayerPrefs.DeleteKey(key + suffix);
            }
            foreach (var pair in savedTimes) PlayerPrefs.SetFloat(pair.Key, pair.Value);
            foreach (var pair in savedInts) PlayerPrefs.SetInt(pair.Key, pair.Value);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator AutoRunStartsWithoutDirectionInputAndStopsForPauseAndDeath()
        {
            PlayerMotor player = session.Player;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            player.enabled = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.EqualTo(player.RunSpeed).Within(0.01f));
            Assert.That(player.FacingDirection, Is.EqualTo(1f));
            session.SetPaused(true);
            Vector2 stopped = body.position;
            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(body.position, Is.EqualTo(stopped));
            session.SetPaused(false);
            yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.GreaterThan(0f));
            session.Die();
            Assert.That(body.velocity, Is.EqualTo(Vector2.zero));
            Assert.That(body.simulated, Is.False);
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
            Assert.That(session.Player.GetComponent<Rigidbody2D>().position.x, Is.EqualTo(2f).Within(0.05f), "Physics spawn");
            Assert.That(session.Player.transform.position.x, Is.EqualTo(2f).Within(0.05f), "Rendered spawn must match physics before the next frame");
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
        public IEnumerator FrozenRespawnKeepsTheNewPositionWhenSimulationResumes()
        {
            PlayerMotor player = session.Player;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            player.SetFrozen(true);
            player.ResetAt(new Vector2(2f, 3f));
            player.SetFrozen(false);
            Assert.That(body.position.x, Is.EqualTo(2f).Within(0.01f));
            Assert.That(player.transform.position.x, Is.EqualTo(2f).Within(0.01f));
            yield return new WaitForFixedUpdate();
            Assert.That(body.position.x, Is.EqualTo(2f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator RespawnHidesOnlyTheSessionsOwnRobot()
        {
            GameObject extra = new GameObject("Independent robot visual");
            extra.transform.position = new Vector2(100f, 50f);
            PlayerVisual extraVisual = extra.AddComponent<PlayerVisual>();
            PlayerVisual playerVisual = session.Player.GetComponent<PlayerVisual>();
            session.Die();
            Assert.That(session.Respawning, Is.True);
            playerVisual.SendMessage("Update");
            extraVisual.SendMessage("Update");
            Assert.That(session.Player.GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(extra.GetComponent<SpriteRenderer>().enabled, Is.True);
            yield return WaitForRespawn();
            playerVisual.SendMessage("Update");
            Assert.That(session.Player.GetComponent<SpriteRenderer>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator ExtraRobotTriggersCannotChangeTheActiveRun()
        {
            GameObject extra = new GameObject("Extra prefab robot");
            extra.transform.position = new Vector2(100f, 50f);
            extra.AddComponent<PlayerMotor>();
            GameObject danger = new GameObject("Extra robot hazard callback");
            danger.transform.position = new Vector2(200f, 50f);
            Collider2D hazardCollider = danger.AddComponent<BoxCollider2D>();
            danger.AddComponent<Hazard>();
            // Dispatch the Unity callbacks directly: this checks session ownership,
            // not the physics engine's overlap or callback timing.
            extra.SendMessage("OnTriggerEnter2D", hazardCollider);
            Assert.That(session.Deaths, Is.Zero);
            Checkpoint checkpoint = CreateCheckpoint(new Vector2(200f, 55f), 500);
            Collider2D checkpointCollider = checkpoint.gameObject.AddComponent<BoxCollider2D>();
            extra.SendMessage("OnTriggerEnter2D", checkpointCollider);
            Assert.That(session.HasCheckpoint, Is.False);
            GameObject exit = new GameObject("Extra robot exit callback");
            exit.transform.position = new Vector2(200f, 60f);
            Collider2D exitCollider = exit.AddComponent<BoxCollider2D>();
            exit.AddComponent<ExitGate>();
            extra.SendMessage("OnTriggerEnter2D", exitCollider);
            Assert.That(session.Finished, Is.False);
            yield break;
        }

        [UnityTest]
        public IEnumerator ScaledMagnetRingMatchesForceRangeAndDisabledState()
        {
            GameObject obj = new GameObject("Scaled magnet");
            obj.transform.position = new Vector2(12f, 4f);
            obj.transform.localScale = new Vector3(2f, 0.5f, 1f);
            obj.transform.rotation = Quaternion.Euler(0f, 0f, 37f);
            MagnetAnchor anchor = obj.AddComponent<MagnetAnchor>();
            anchor.Configure(3f, 1f);
            LineRenderer ring = obj.GetComponent<LineRenderer>();
            Assert.That(anchor.Contains((Vector2)obj.transform.position + Vector2.right * 3f, 7f), Is.True);
            Assert.That(anchor.Contains((Vector2)obj.transform.position + Vector2.right * 3.1f, 7f), Is.False);
            yield return null;
            for (int change = 0; change < 2; change++)
            {
                for (int i = 0; i < ring.positionCount; i++)
                {
                    Vector2 offset = obj.transform.TransformPoint(ring.GetPosition(i)) - obj.transform.position;
                    Assert.That(offset.magnitude, Is.EqualTo(3f).Within(0.001f));
                }
                obj.transform.localScale = new Vector3(0.75f, 2.5f, 1f);
                yield return null;
            }
            anchor.enabled = false;
            Assert.That(ring.enabled, Is.False);
            Assert.That(MagnetAnchor.All.Contains(anchor), Is.False);
            anchor.enabled = true;
            Assert.That(ring.enabled, Is.True);
            Assert.That(MagnetAnchor.All.Contains(anchor), Is.True);
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

        [UnityTest]
        public IEnumerator HeldJumpRisesHigherThanReleasedJump()
        {
            PlayerMotor player = session.Player;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            float[] heights = new float[2];
            for (int attempt = 0; attempt < 2; attempt++)
            {
                player.ResetAt(new Vector2(0f, 0.25f));
                Physics2D.SyncTransforms();
                for (int step = 0; step < 5; step++)
                {
                    player.SendMessage("FixedUpdate");
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(player.Grounded, Is.True);
                SetMotorField(player, "jumpHeld", true);
                SetMotorField(player, "jumpQueued", true);
                for (int step = 0; step < 42; step++)
                {
                    if (attempt == 0 && step == 2)
                    {
                        SetMotorField(player, "jumpHeld", false);
                        SetMotorField(player, "cutJump", true);
                    }
                    player.SendMessage("FixedUpdate");
                    yield return new WaitForFixedUpdate();
                    heights[attempt] = Mathf.Max(heights[attempt], body.position.y);
                }
            }
            Assert.That(heights[0], Is.GreaterThan(0.5f), "A tap must leave the floor.");
            Assert.That(heights[1], Is.GreaterThan(heights[0] + 1f), "Holding Space must meaningfully increase height.");
        }

        [UnityTest]
        public IEnumerator ForwardDashRejectsRepeatedRequestsUntilCooldownAndRespawnResetsIt()
        {
            PlayerMotor player = session.Player;
            player.DashEnabled = true;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            player.ResetAt(new Vector2(0f, 4f));
            SetMotorField(player, "dashQueued", true);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsDashing, Is.True);
            Assert.That(body.velocity.x, Is.GreaterThan(player.RunSpeed));
            Assert.That(body.velocity.y, Is.Zero);
            for (int step = 0; step < 12; step++)
            {
                SetMotorField(player, "dashQueued", true);
                player.SendMessage("FixedUpdate");
                yield return new WaitForFixedUpdate();
            }
            Assert.That(player.IsDashing, Is.False, "Holding/repeating Shift cannot extend a dash.");
            Assert.That(player.Cooldown, Is.GreaterThan(0f));
            Assert.That(body.gravityScale, Is.EqualTo(3f));
            session.Die();
            yield return WaitForRespawn();
            Assert.That(player.Cooldown, Is.Zero);
            Assert.That(player.IsDashing, Is.False);
            player.SendMessage("FixedUpdate");
            Assert.That(body.velocity.x, Is.EqualTo(7f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator AirMagnetChangesOnlyVerticalSpeedAndReleasesOutsideRange()
        {
            PlayerMotor player = session.Player;
            player.MagnetEnabled = true;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            GameObject obj = new GameObject("Flight test magnet");
            MagnetAnchor anchor = obj.AddComponent<MagnetAnchor>();
            anchor.Configure(7f, 1f);
            foreach (int side in new[] { -1, 1 })
            foreach (int mode in new[] { -1, 1 })
            {
                player.ResetAt(new Vector2(0f, 8f));
                obj.transform.position = new Vector2(-3f, 8f + side * 3f);
                Physics2D.SyncTransforms();
                SetMotorField(player, "magnetMode", mode);
                player.SendMessage("FixedUpdate");
                Assert.That(body.velocity.x, Is.EqualTo(7f).Within(0.01f));
                Assert.That(body.velocity.y * side * mode, Is.GreaterThan(0f));
                Assert.That(player.MagnetTarget, Is.EqualTo(obj.transform));
                obj.transform.position = new Vector2(-20f, 30f);
                body.velocity = Vector2.zero;
                player.SendMessage("FixedUpdate");
                Assert.That(player.MagnetTarget, Is.Null);
                Assert.That(body.velocity.y, Is.Zero, "Out-of-range magnet must add no force.");
            }
            Object.Destroy(obj);
            yield break;
        }

        [UnityTest]
        public IEnumerator SourceCoursesHaveAForwardOnlyDeathlessRoute()
        {
            // Test-only driver supplies the same queued commands as Update. The
            // real motor, colliders, drones, checkpoints and exit run in Unity.
            // This proves a route exists; it does not replace human keyboard QA.
            for (int stage = 1; stage <= 4; stage++)
            {
                SceneManager.LoadScene("Stage" + stage);
                yield return null;
                session = GameSession.Active;
                session.SetPaused(false);
                PlayerMotor player = session.Player;
                player.enabled = false;
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                StageDefinition layout = StageInfo.Definition(stage);
                float jumpedAt = -100f;
                float lastJumpX = -100f;
                bool gapJump = false;
                float deadline = Time.realtimeSinceStartup + 60f;
                Time.timeScale = 5f;
                while (!session.Finished && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(session.Deaths, Is.Zero,
                        "Stage " + stage + " crashed near x=" + body.position.x + ", y=" + body.position.y);
                    float x = body.position.x;
                    bool onFloor = player.Grounded;
                    float edge = float.PositiveInfinity;
                    float gap = 0f;
                    for (int i = 0; i < layout.Platforms.Length - 1; i++)
                    {
                        float right = layout.Platforms[i].Position.x + layout.Platforms[i].Width * 0.5f;
                        if (right < x - 0.5f) continue;
                        edge = right;
                        gap = layout.Platforms[i + 1].Position.x - layout.Platforms[i + 1].Width * 0.5f - right;
                        break;
                    }
                    bool jumpGap = edge - x < 0.8f && edge - x > -0.3f;
                    bool jumpBarrier = false;
                    foreach (StageDefinition.RectPlacement spike in layout.Spikes)
                    {
                        float ahead = spike.Position.x - spike.Width * 0.5f - x;
                        if (ahead > 1.6f && ahead < 1.8f) jumpBarrier = true;
                    }
                    if (onFloor && x - lastJumpX > 2f && (jumpGap || jumpBarrier))
                    {
                        SetMotorField(player, "jumpQueued", true);
                        SetMotorField(player, "jumpHeld", true);
                        jumpedAt = Time.time;
                        lastJumpX = x;
                        gapJump = jumpGap && gap >= 7f;
                        Debug.Log("Route jump stage=" + stage + " x=" + x + " gap=" + (jumpGap ? gap : 0f));
                    }
                    if (gapJump && Time.time - jumpedAt >= 0.36f && player.Cooldown <= 0f)
                    {
                        SetMotorField(player, "dashQueued", true);
                        gapJump = false;
                    }
                    int mode = 0;
                    if (!onFloor && body.position.y < 4.5f && player.MagnetEnabled)
                    {
                        MagnetAnchor closest = null;
                        float distance = float.PositiveInfinity;
                        foreach (MagnetAnchor anchor in MagnetAnchor.All)
                        {
                            float candidate = Vector2.Distance(body.position, anchor.transform.position);
                            if (candidate < distance && anchor.Contains(body.position, player.MagnetReach))
                            { closest = anchor; distance = candidate; }
                        }
                        if (player.MagnetTarget != null) mode = player.MagnetTarget.position.y > body.position.y ? 1 : -1;
                        else if (closest != null) mode = closest.transform.position.y > body.position.y ? 1 : -1;
                    }
                    SetMotorField(player, "magnetMode", mode);
                    player.SendMessage("FixedUpdate");
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(session.Finished, Is.True, "Stage " + stage + " did not reach the real ExitGate at x=" + body.position.x);
                Assert.That(session.Deaths, Is.Zero);
                Debug.Log("Forward route Stage " + stage + ": " + session.Elapsed.ToString("F2") + "s, no deaths");
                Time.timeScale = 1f;
            }
        }

        private static void SetMotorField(PlayerMotor player, string name, object value)
        {
            typeof(PlayerMotor).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(player, value);
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
