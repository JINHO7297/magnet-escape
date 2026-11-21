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
        private bool sourceSetupReady;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            sourceSetupReady = false;
            // Scene names are shared by editable revisions. Never run source
            // routes (or save scores) against a user-selected revision.
            for (int stage = 1; stage <= StageInfo.Count; stage++)
                if (StageRecords.BuildScenePath(stage) != "Assets/_MagnetEscape/Scenes/Stage" + stage + ".unity")
                    Assert.Ignore("Source gameplay tests require the four generated source stages in Build Settings.");
            sourceSetupReady = true;
            savedTimes.Clear();
            savedInts.Clear();
            for (int stage = 1; stage <= StageInfo.Count; stage++)
            {
                string key = StageInfo.RecordPrefix(stage) + stage;
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
            if (!sourceSetupReady) yield break;
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            SceneManager.LoadScene("StageSelect");
            yield return null;
            for (int stage = 1; stage <= StageInfo.Count; stage++)
            {
                string key = StageInfo.RecordPrefix(stage) + stage;
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
            for (int stage = 1; stage <= StageInfo.Count; stage++)
            {
                SceneManager.LoadScene("Stage" + stage);
                yield return null;
                GameSession active = GameSession.Active;
                Assert.That(active, Is.Not.Null);
                active.SetPaused(false);
                active.Player.enabled = false;
                Assert.That(active.Stage, Is.EqualTo(stage));
                Assert.That(active.Player.DashEnabled, Is.EqualTo(stage != 1));
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

        [UnityTest, Timeout(900000)]
        public IEnumerator SourceCoursesHaveAForwardOnlyDeathlessRoute()
        {
            // Test-only driver supplies the same queued commands as Update. The
            // real motor, colliders, drones, checkpoints and exit run in Unity.
            // This proves a route exists; it does not replace human keyboard QA.
            for (int slot = 1; slot <= StageInfo.FestivalCount; slot++)
            {
                int stage = StageInfo.FestivalForSlot(slot);
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
                int grindSteps = 0;
                float deadline = Time.time + 150f;
                Time.captureDeltaTime = Time.fixedDeltaTime;
                Time.timeScale = 1f;
                while (!session.Finished && Time.time < deadline)
                {
                    Assert.That(session.Paused, Is.False, "Route test lost gameplay focus.");
                    Assert.That(Time.timeScale, Is.GreaterThan(0f));
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
                    if (player.RailEnabled && !onFloor)
                    {
                        Bounds shape = player.GetComponent<BoxCollider2D>().bounds;
                        foreach (MagneticRail rail in MagneticRail.All)
                            if (rail.CanCatch(shape.center, shape.extents.y)) { mode = 1; break; }
                        if (player.IsGrinding)
                        {
                            grindSteps++;
                            mode = 1;
                            foreach (RotatingSaw drone in Object.FindObjectsOfType<RotatingSaw>())
                            {
                                float ahead = drone.transform.position.x - body.position.x;
                                if (ahead > 1.7f && ahead < 2.2f && Mathf.Abs(drone.transform.position.y - body.position.y) < 1.2f)
                                {
                                    SetMotorField(player, "jumpQueued", true);
                                    SetMotorField(player, "jumpHeld", true);
                                }
                            }
                        }
                    }
                    SetMotorField(player, "magnetMode", mode);
                    player.SendMessage("FixedUpdate");
                    yield return null;
                }
                Assert.That(session.Finished, Is.True, "Stage " + stage + " did not reach the real ExitGate at x=" + body.position.x);
                Assert.That(session.Deaths, Is.Zero);
                Assert.That(grindSteps, Is.GreaterThan(0), "Both festival routes must exercise magnetic rails.");
                Debug.Log("Forward route Stage " + stage + ": " + session.Elapsed.ToString("F2") + "s, no deaths, rail steps=" + grindSteps);
                Time.captureDeltaTime = 0f;
                Time.timeScale = 1f;
            }
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator EverySourceCheckpointAllowsOneSecondOfUninterruptedForwardPreparation()
        {
            for (int stage = 1; stage <= StageInfo.Count; stage++)
            {
                SceneManager.LoadScene("Stage" + stage);
                yield return null;
                yield return null;
                session = GameSession.Active;
                session.SetPaused(false);
                PlayerMotor player = session.Player;
                player.enabled = false;
                Checkpoint[] checkpoints = Object.FindObjectsOfType<Checkpoint>();
                System.Array.Sort(checkpoints, (a, b) => a.Order.CompareTo(b.Order));
                Time.timeScale = 5f;
                foreach (Checkpoint checkpoint in checkpoints)
                {
                    session.SetCheckpoint(checkpoint);
                    Assert.That(session.CheckpointNumber, Is.EqualTo(checkpoint.Order));
                    session.Die();
                    int deaths = session.Deaths;
                    yield return WaitForRespawn();
                    Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                    Assert.That(body.position.x, Is.EqualTo(checkpoint.SpawnPoint.x).Within(0.01f));
                    for (int step = 0; step < 50; step++)
                    {
                        player.SendMessage("FixedUpdate");
                        yield return new WaitForFixedUpdate();
                        Assert.That(session.Deaths, Is.EqualTo(deaths), "Unsafe spawn: stage " + stage + " CP " + checkpoint.Order);
                    }
                    Assert.That(body.position.x, Is.GreaterThan(checkpoint.SpawnPoint.x + 6.5f),
                        "Blocked forward preparation: stage " + stage + " CP " + checkpoint.Order);
                    Assert.That(player.Grounded, Is.True);
                }
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator DashingIntoAHazardStillCountsOneDeath()
        {
            PlayerMotor player = session.Player;
            player.DashEnabled = true;
            player.ResetAt(new Vector2(0f, 4f));
            GameObject obstacle = new GameObject("Dash collision regression");
            obstacle.transform.position = new Vector2(1f, 4f);
            BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            obstacle.AddComponent<Hazard>();
            Physics2D.SyncTransforms();
            SetMotorField(player, "dashQueued", true);
            for (int step = 0; step < 5 && !session.Respawning; step++)
            {
                player.SendMessage("FixedUpdate");
                yield return new WaitForFixedUpdate();
            }
            Assert.That(session.Respawning, Is.True);
            Assert.That(session.Deaths, Is.EqualTo(1));
            Assert.That(player.CanMove, Is.False);
            Assert.That(player.GetComponent<Rigidbody2D>().simulated, Is.False);
            Object.Destroy(obstacle);
        }

        [UnityTest]
        public IEnumerator RailRequiresAttractInputAndSupportsReleaseAndJumpLaunch()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            MagneticRail rail = CreateRail();
            player.ResetAt(new Vector2(100f, 8f));
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.False);
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.True);
            Assert.That(body.velocity.x, Is.GreaterThan(7f));
            Assert.That(body.gravityScale, Is.Zero);
            SetMotorField(player, "magnetMode", 0);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.False);
            Assert.That(body.gravityScale, Is.EqualTo(3f));
            player.ResetAt(new Vector2(100f, 8f));
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            SetMotorField(player, "jumpQueued", true);
            SetMotorField(player, "jumpHeld", true);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.False);
            Assert.That(body.velocity.y, Is.GreaterThan(10f));
            Assert.That(body.velocity.x, Is.GreaterThan(0f));
            Object.Destroy(rail.gameObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator RailRepelEndAndFreezeReleaseTheBoardWithoutReversingIt()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            MagneticRail rail = CreateRail();
            foreach (int exit in new[] { 0, 1, 2 })
            {
                player.ResetAt(new Vector2(100f, 8f));
                SetMotorField(player, "magnetMode", 1);
                player.SendMessage("FixedUpdate");
                Assert.That(player.IsGrinding, Is.True);
                if (exit == 0) SetMotorField(player, "magnetMode", -1);
                else if (exit == 1) body.position = new Vector2(111f, 8f);
                else player.SetFrozen(true);
                player.SendMessage("FixedUpdate");
                Assert.That(player.IsGrinding, Is.False);
                Assert.That(body.gravityScale, Is.EqualTo(3f));
                if (exit == 0) Assert.That(body.velocity.y, Is.GreaterThan(10f));
                if (exit != 2) Assert.That(body.velocity.x, Is.GreaterThan(0f));
                player.SetFrozen(false);
            }
            Object.Destroy(rail.gameObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator RailCannotProtectThePlayerFromHazards()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            player.ResetAt(new Vector2(100f, 8f));
            MagneticRail rail = CreateRail();
            GameObject danger = new GameObject("Rail danger");
            danger.transform.position = new Vector2(100.5f, 8f);
            danger.AddComponent<BoxCollider2D>().isTrigger = true;
            danger.AddComponent<Hazard>();
            Physics2D.SyncTransforms();
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(session.Deaths, Is.EqualTo(1));
            Assert.That(player.IsGrinding, Is.False);
            Assert.That(player.CanMove, Is.False);
            Object.Destroy(danger);
            Object.Destroy(rail.gameObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator InclinedRailCarriesTheBoardThroughPhysicsAndDashDetaches()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = player.DashEnabled = true;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            MagneticRail rail = CreateRail();
            rail.Configure(new Vector2(99f, 7.55f), new Vector2(110f, 8.65f), 1.1f);
            player.ResetAt(new Vector2(100f, 8.1f));
            for (int step = 0; step < 20; step++)
            {
                SetMotorField(player, "magnetMode", 1);
                player.SendMessage("FixedUpdate");
                yield return new WaitForFixedUpdate();
            }
            Assert.That(player.IsGrinding, Is.True);
            Assert.That(body.position.x, Is.GreaterThan(103f));
            Assert.That(body.position.y, Is.GreaterThan(8.3f));
            session.SetPaused(true);
            Vector2 stopped = body.position;
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(body.position, Is.EqualTo(stopped));
            session.SetPaused(false);
            SetMotorField(player, "dashQueued", true);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.False);
            Assert.That(player.IsDashing, Is.True);
            Assert.That(body.velocity.x, Is.EqualTo(19f).Within(0.01f));
            Object.Destroy(rail.gameObject);
        }

        [UnityTest]
        public IEnumerator RailCaptureCannotSkipAnInterveningHazard()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            player.ResetAt(new Vector2(100f, 8f));
            MagneticRail rail = CreateRail();
            rail.Configure(new Vector2(99f, 5.55f), new Vector2(110f, 5.55f), 3f);
            GameObject danger = new GameObject("Thin hazard between board and rail");
            danger.transform.position = new Vector2(100f, 7f);
            danger.AddComponent<BoxCollider2D>().size = new Vector2(2f, 0.08f);
            danger.GetComponent<BoxCollider2D>().isTrigger = true;
            danger.AddComponent<Hazard>();
            Physics2D.SyncTransforms();
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(session.Deaths, Is.EqualTo(1));
            Assert.That(player.IsGrinding, Is.False);
            Object.Destroy(danger);
            Object.Destroy(rail.gameObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator RailCaptureIsBlockedBySolidGround()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            player.ResetAt(new Vector2(100f, 8f));
            MagneticRail rail = CreateRail();
            rail.Configure(new Vector2(99f, 5.55f), new Vector2(110f, 5.55f), 3f);
            GameObject floor = new GameObject("Floor between board and rail");
            floor.layer = LayerMask.NameToLayer("Ground");
            floor.transform.position = new Vector2(100f, 7f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(2f, 0.08f);
            Physics2D.SyncTransforms();
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.False);
            Assert.That(player.GetComponent<Rigidbody2D>().position.y, Is.EqualTo(8f).Within(0.01f));
            Object.Destroy(floor);
            Object.Destroy(rail.gameObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator PauseResumePreservesCollectedHeldMagnetState()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            MagneticRail rail = CreateRail();
            foreach (bool held in new[] { true, false })
            {
                player.ResetAt(new Vector2(100f, 8f));
                SetMotorField(player, "magnetMode", 1);
                player.SendMessage("FixedUpdate");
                Assert.That(player.IsGrinding, Is.True);
                session.SetPaused(true);
                // Model the held level collected by motor Update while paused.
                // This does not synthesize a physical key event.
                SetMotorField(player, "magnetMode", held ? 1 : 0);
                yield return new WaitForSecondsRealtime(0.02f);
                session.SetPaused(false);
                player.SendMessage("FixedUpdate");
                Assert.That(player.IsGrinding, Is.EqualTo(held));
            }
            Object.Destroy(rail.gameObject);
        }

        [UnityTest]
        public IEnumerator RailAlignsTheActualBottomOfAnOffsetScaledCollider()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            BoxCollider2D shape = player.GetComponent<BoxCollider2D>();
            shape.offset = new Vector2(0.3f, 0.6f);
            player.transform.localScale = new Vector3(1.2f, 1.5f, 1f);
            player.ResetAt(new Vector2(100f, 8f));
            MagneticRail rail = CreateRail();
            Physics2D.SyncTransforms();
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.True);
            Assert.That(player.GetComponent<Rigidbody2D>().position.y, Is.EqualTo(7.325f).Within(0.02f));
            Assert.That(shape.bounds.min.y, Is.EqualTo(7.55f).Within(0.02f));
            Object.Destroy(rail.gameObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator FinalDashStepStopsAtAWallButNotAtTheSupportingFloor()
        {
            PlayerMotor player = session.Player;
            player.DashEnabled = true;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            GameObject solid = new GameObject("Final dash step solid");
            solid.layer = LayerMask.NameToLayer("Ground");
            BoxCollider2D obstacle = solid.AddComponent<BoxCollider2D>();
            foreach (bool wall in new[] { true, false })
            {
                solid.transform.position = wall ? new Vector2(103.4f, 8f) : new Vector2(103f, 7.3f);
                obstacle.size = wall ? new Vector2(0.5f, 4f) : new Vector2(12f, 0.5f);
                player.ResetAt(new Vector2(100f, 8f));
                Physics2D.SyncTransforms();
                SetMotorField(player, "dashQueued", true);
                for (int step = 0; step < 8; step++)
                {
                    player.SendMessage("FixedUpdate");
                    yield return new WaitForFixedUpdate();
                    if (step < 7) Assert.That(player.IsDashing, Is.True, "Contact must occur on the final step.");
                }
                yield return null; // Receive the final physics step's collision callbacks.
                Assert.That(player.IsDashing, Is.EqualTo(!wall), "Only a facing wall cancels the last dash step.");
                Assert.That(body.gravityScale, Is.EqualTo(wall ? 3f : 0f));
                Assert.That(player.Cooldown, Is.GreaterThan(0f));
                Assert.That(session.Deaths, Is.Zero);
                if (wall) Assert.That(body.position.x, Is.LessThan(102.85f));
                player.SendMessage("FixedUpdate");
                Assert.That(player.IsDashing, Is.False);
                Assert.That(body.gravityScale, Is.EqualTo(3f));
            }
            Object.Destroy(solid);
        }

        [UnityTest]
        public IEnumerator ReleasingMagnetInsideRangeStopsForceAndItsActivePresentation()
        {
            PlayerMotor player = session.Player;
            player.MagnetEnabled = true;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            GameObject obj = new GameObject("In-range magnet release");
            MagnetAnchor anchor = obj.AddComponent<MagnetAnchor>();
            obj.transform.position = new Vector2(100f, 11f);
            anchor.Configure(7f, 1f);
            foreach (int mode in new[] { 1, -1 })
            {
                player.ResetAt(new Vector2(100f, 8f));
                Physics2D.SyncTransforms();
                SetMotorField(player, "magnetMode", mode);
                player.SendMessage("FixedUpdate");
                Assert.That(body.velocity.y * mode, Is.GreaterThan(0f));
                Assert.That(anchor.Contains(body.position, player.MagnetReach), Is.True);
                anchor.SendMessage("Update");
                LineRenderer ring = obj.GetComponent<LineRenderer>();
                Assert.That(ring.startColor.a, Is.EqualTo(0.65f).Within(0.01f));
                Assert.That(ring.startWidth, Is.EqualTo(0.05f).Within(0.001f));
                PlayerVisual visual = player.GetComponent<PlayerVisual>();
                // SendMessage dispatches to every component on the GameObject,
                // including motor.Update, which would replace the test command with headless Input.
                var updateVisual = typeof(PlayerVisual).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                updateVisual.Invoke(visual, null);
                LineRenderer tether = player.GetComponent<LineRenderer>();
                Assert.That(tether.enabled, Is.True);
                Assert.That(tether.GetPosition(0), Is.EqualTo(player.transform.position));
                Assert.That(tether.GetPosition(1), Is.EqualTo(obj.transform.position));
                SetMotorField(player, "magnetMode", 0);
                body.velocity = Vector2.zero;
                player.SendMessage("FixedUpdate");
                Assert.That(body.velocity, Is.EqualTo(new Vector2(7f, 0f)));
                Assert.That(body.gravityScale, Is.EqualTo(3f));
                anchor.SendMessage("Update");
                Assert.That(ring.startColor.a, Is.EqualTo(0.35f).Within(0.01f), "The selected ring stays visible without the active force highlight.");
                Assert.That(ring.startWidth, Is.EqualTo(0.025f).Within(0.001f));
                updateVisual.Invoke(visual, null);
                Assert.That(tether.enabled, Is.False, "Releasing Q/E removes the active force connection inside range.");
            }
            Object.Destroy(obj);
            yield break;
        }

        [UnityTest]
        public IEnumerator JumpReleasedDuringPauseCutsAscentOnTheFirstResumedStep()
        {
            PlayerMotor player = session.Player;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            player.ResetAt(new Vector2(100f, 8f));
            body.velocity = new Vector2(7f, 10f);
            SetMotorField(player, "jumpHeld", true);
            session.SetPaused(true);
            player.SendMessage("Update"); // Headless Input reports Space released, while physics is paused.
            Assert.That(body.velocity.y, Is.EqualTo(10f));
            session.SetPaused(false);
            player.SendMessage("Update");
            player.SendMessage("FixedUpdate");
            Assert.That(body.velocity.y, Is.EqualTo(4.5f).Within(0.01f));
            Assert.That(body.velocity.x, Is.EqualTo(7f));
            player.SendMessage("Update");
            player.SendMessage("FixedUpdate");
            Assert.That(body.velocity.y, Is.EqualTo(4.5f).Within(0.01f), "The release cut is applied once.");
            yield break;
        }

        [UnityTest]
        public IEnumerator DescendingRailNaturallyEndsAndRestoresForwardFlight()
        {
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            MagneticRail rail = CreateRail();
            rail.Configure(new Vector2(99f, 7.65f), new Vector2(103f, 7.25f), 1.1f);
            player.ResetAt(new Vector2(100f, 8f));
            SetMotorField(player, "magnetMode", 1);
            bool caught = false;
            for (int step = 0; step < 22; step++)
            {
                player.SendMessage("FixedUpdate");
                caught |= player.IsGrinding;
                if (player.IsGrinding)
                {
                    Assert.That(body.velocity.x, Is.EqualTo(9.45f).Within(0.01f));
                    Assert.That(body.velocity.y, Is.LessThan(0f));
                }
                yield return new WaitForFixedUpdate();
            }
            Assert.That(caught, Is.True);
            Assert.That(player.IsGrinding, Is.False, "Movement must naturally cross the end without teleporting.");
            Assert.That(body.position.x, Is.GreaterThan(103f));
            Assert.That(body.velocity.x, Is.EqualTo(7f).Within(0.01f));
            Assert.That(body.gravityScale, Is.EqualTo(3f));
            Assert.That(session.Deaths, Is.Zero);
            Object.Destroy(rail.gameObject);
        }

        [UnityTest]
        public IEnumerator ShapedRoutesRequireNearbyInputAndBuildRealDistinctPaths()
        {
            var obj = new GameObject("Route shaping regression");
            RouteShaper route = obj.AddComponent<RouteShaper>();
            route.Configure(new Vector2(108f, 8f), new Vector2(122f, 11f), new Vector2(122f, 8f), "광고판");
            PlayerMotor player = session.Player;
            player.ResetAt(new Vector2(90f, 8f));
            Assert.That(route.TryShape(player, 1), Is.False, "Remote construction is forbidden.");
            player.ResetAt(new Vector2(103f, 8f));
            Assert.That(route.TryShape(player, 0), Is.False);
            Assert.That(route.TryShape(player, 1), Is.True);
            Assert.That(route.Rail.EndPoint, Is.EqualTo(new Vector2(122f, 11f)));
            Assert.That(route.Rail.enabled, Is.True);
            Assert.That(route.Bridge.enabled, Is.False);
            Assert.That(route.TryShape(player, -1), Is.True);
            Assert.That(route.Bridge.enabled, Is.True);
            Assert.That(route.Bridge.isTrigger, Is.False);
            Assert.That(route.Bridge.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Ground")));
            Assert.That(route.Rail.enabled, Is.False);
            session.SetPaused(true);
            Assert.That(route.TryShape(player, 1), Is.False);
            session.SetPaused(false);
            player.ResetAt(new Vector2(108f, 8f));
            Assert.That(route.TryShape(player, 1), Is.False, "No moving geometry under an occupied route.");
            player.ResetAt(new Vector2(103f, 8f));
            session.Die();
            Assert.That(route.TryShape(player, 1), Is.False);
            Object.Destroy(obj);
            yield break;
        }

        [UnityTest]
        public IEnumerator ConstructedBridgeSupportsRealPhysicsAndUpperRailCanBeRidden()
        {
            var obj = new GameObject("Playable shaped route");
            RouteShaper route = obj.AddComponent<RouteShaper>();
            route.Configure(new Vector2(108f, 7.55f), new Vector2(122f, 9.55f), new Vector2(122f, 7.55f), "철제 다리");
            PlayerMotor player = session.Player;
            player.ResetAt(new Vector2(103f, 8f));
            Assert.That(route.TryShape(player, -1), Is.True);
            player.ResetAt(new Vector2(110f, 8.1f));
            for (int step = 0; step < 15; step++)
            {
                player.SendMessage("FixedUpdate");
                yield return new WaitForFixedUpdate();
            }
            Assert.That(player.Grounded, Is.True);
            Assert.That(session.Deaths, Is.Zero);
            player.ResetAt(new Vector2(103f, 8f));
            Assert.That(route.TryShape(player, 1), Is.True);
            player.RailEnabled = true;
            player.ResetAt(new Vector2(109f, 8.3f));
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.True);
            Assert.That(player.GetComponent<Rigidbody2D>().velocity.x, Is.GreaterThan(player.RunSpeed));
            Object.Destroy(obj);
        }

        [UnityTest]
        public IEnumerator BillboardHoldSetsAnIntermediateAngleAndLocksBeforeEntry()
        {
            var root = new GameObject("Billboard");
            var route = root.AddComponent<RouteShaper>();
            route.Configure(new Vector2(108, 8), new Vector2(120, 11), new Vector2(120, 8),
                "회전 광고판", RouteShaper.Kind.Billboard);
            PlayerMotor player = session.Player;
            player.ResetAt(new Vector2(103, 8));
            route.TryShape(player, 1, 0.35f);
            Assert.That(route.Charge, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(route.Rail.EndPoint.y, Is.EqualTo(9.5f).Within(0.01f));
            Assert.That(route.TryShape(player, 0), Is.False);
            route.TryShape(player, 1, 0.35f);
            Assert.That(route.Rail.EndPoint.y, Is.EqualTo(11f));
            player.ResetAt(new Vector2(107, 8));
            Assert.That(route.TryShape(player, -1), Is.False);
            player.ResetAt(new Vector2(103, 8));
            Assert.That(route.TryShape(player, -1), Is.True);
            Assert.That(route.Bridge.enabled, Is.True);
            Object.Destroy(root);
            yield break;
        }

        [UnityTest]
        public IEnumerator EasyMiddleDeckCanBeReachedFromTheStreet()
        {
            SceneManager.LoadScene("Stage8");
            yield return null;
            session = GameSession.Active;
            session.SetPaused(false);
            var player = session.Player;
            player.enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            var deck = StageInfo.Definition(8).MidDecks[0];
            player.ResetAt(new Vector2(deck.Position.x - deck.Width * .5f, .25f));
            Time.captureDeltaTime = Time.fixedDeltaTime;
            player.SendMessage("FixedUpdate");
            Assert.That(player.Grounded, Is.True);
            SetMotorField(player, "jumpQueued", true);
            SetMotorField(player, "jumpHeld", true);
            bool landed = false;
            for (int step = 0; step < 60 && !landed; step++)
            {
                player.SendMessage("FixedUpdate");
                yield return null;
                landed = player.Grounded && Mathf.Abs(body.position.y - deck.Position.y - .75f) < .12f;
            }
            Assert.That(landed, Is.True, "Jump through the underside and land on the middle deck.");
            Assert.That(session.Deaths, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FestivalCeilingSupportsInvertedRunningJumpAndRelease()
        {
            SceneManager.LoadScene("Stage8");
            yield return null;
            session = GameSession.Active;
            session.SetPaused(false);
            var player = session.Player;
            player.enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            var ceiling = StageInfo.Definition(8).InversionCeilings[0];
            float entry = ceiling.Position.x - ceiling.Width * .5f;
            player.ResetAt(new Vector2(entry + 1f, ceiling.Position.y - 2.8f));
            Time.captureDeltaTime = Time.fixedDeltaTime;
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsInverted, Is.True);
            SetMotorField(player, "magnetMode", 0);
            for (int step = 0; step < 50 && !player.Grounded; step++)
            {
                yield return null;
                player.SendMessage("FixedUpdate");
            }
            Assert.That(player.Grounded && player.IsInverted, Is.True);
            Assert.That(body.position.y, Is.EqualTo(ceiling.Position.y - .75f).Within(.15f));
            Assert.That(body.velocity.x, Is.GreaterThan(0f));
            SetMotorField(player, "jumpQueued", true);
            SetMotorField(player, "jumpHeld", true);
            player.SendMessage("FixedUpdate");
            Assert.That(body.velocity.y, Is.LessThan(-5f));
            Assert.That(body.gravityScale, Is.LessThan(0f));
            SetMotorField(player, "magnetMode", -1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsInverted, Is.False);
            Assert.That(body.gravityScale, Is.GreaterThan(0f));

            player.ResetAt(new Vector2(entry + 1f, ceiling.Position.y - 2.8f));
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsInverted, Is.True);
            SetMotorField(player, "magnetMode", 0);
            for (int step = 0; step < 240 && player.IsInverted; step++)
            {
                yield return null;
                player.SendMessage("FixedUpdate");
            }
            Assert.That(session.Deaths, Is.Zero);
            Assert.That(body.position.x, Is.GreaterThanOrEqualTo(entry + ceiling.Width));
            Assert.That(player.IsInverted, Is.False);
            Assert.That(body.gravityScale, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator SpaceWhileGrindingTransfersToOverlappingCable()
        {
            SceneManager.LoadScene("Stage8");
            yield return null;
            session = GameSession.Active;
            session.SetPaused(false);
            PlayerMotor player = session.Player;
            player.enabled = false;
            RouteShaper board = null;
            foreach (RouteShaper route in RouteShaper.All)
                if (route.Facility == RouteShaper.Kind.Billboard && route.StartPoint.x == 38f) board = route;
            Assert.That(board, Is.Not.Null);
            player.ResetAt(board.StartPoint + Vector2.left * 4f);
            Assert.That(board.TryShape(player, 1, 1f), Is.True);
            float x = board.UpperEnd.x - 1f;
            float halfHeight = player.GetComponent<BoxCollider2D>().bounds.extents.y;
            player.ResetAt(new Vector2(x, board.Rail.HeightAt(x) + halfHeight));
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding && !player.IsCeilingRiding, Is.True);
            SetMotorField(player, "jumpQueued", true);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsCeilingRiding, Is.True);
            Assert.That(player.GetComponent<Rigidbody2D>().velocity.x, Is.GreaterThan(player.RunSpeed));
            SetMotorField(player, "magnetMode", -1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsGrinding, Is.False);
        }

        [UnityTest]
        public IEnumerator CeilingRideHoldsFromBelowAndDropsWithRepulsion()
        {
            var root = new GameObject("Ceiling");
            var route = root.AddComponent<RouteShaper>();
            route.Configure(new Vector2(108, 8), new Vector2(122, 11), new Vector2(122, 8),
                "천장 케이블", RouteShaper.Kind.Ceiling);
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            player.ResetAt(new Vector2(109, 10));
            Assert.That(route.Rail.enabled, Is.True, "Space and Q must work even when first pressed below the cable.");
            SetMotorField(player, "magnetMode", 1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsCeilingRiding, Is.True);
            var body = player.GetComponent<Rigidbody2D>();
            Assert.That(body.position.y, Is.LessThan(route.Rail.HeightAt(109)));
            Assert.That(body.velocity.x, Is.GreaterThanOrEqualTo(player.RunSpeed));
            var landing = new GameObject("Ceiling landing");
            landing.layer = LayerMask.NameToLayer("Ground");
            landing.transform.position = new Vector2(118f, 7.4f);
            landing.AddComponent<BoxCollider2D>().size = new Vector2(14f, .6f);
            Time.captureDeltaTime = Time.fixedDeltaTime;
            for (int step = 0; step < 32; step++)
            {
                player.SendMessage("FixedUpdate");
                yield return null;
            }
            Assert.That(session.FacilitiesCompleted, Is.Zero, "The ceiling trick requires a landing.");
            SetMotorField(player, "magnetMode", -1);
            player.SendMessage("FixedUpdate");
            Assert.That(player.IsCeilingRiding, Is.False);
            Assert.That(body.gravityScale, Is.EqualTo(3f));
            Assert.That(body.velocity.y, Is.LessThanOrEqualTo(0f));
            for (int step = 0; step < 30 && !player.Grounded; step++)
            {
                player.SendMessage("FixedUpdate");
                yield return null;
            }
            Assert.That(player.Grounded, Is.True);
            Assert.That(session.FacilitiesCompleted, Is.EqualTo(1));
            Object.Destroy(landing);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ConvoySupportsTwoActualLandingsAndLetsTheRiderPassBelow()
        {
            var root = new GameObject("Convoy");
            var route = root.AddComponent<RouteShaper>();
            route.Configure(new Vector2(108, 8), new Vector2(130, 9), new Vector2(130, 8),
                "축제 차량", RouteShaper.Kind.Convoy);
            PlayerMotor player = session.Player;
            var body = player.GetComponent<Rigidbody2D>();
            MovingPlatform[] vehicles = root.GetComponentsInChildren<MovingPlatform>();
            Vector2 origin = vehicles[0].transform.position;
            Time.captureDeltaTime = Time.fixedDeltaTime;
            player.ResetAt(new Vector2(108f, 7.15f));
            body.gravityScale = 0f;
            body.velocity = Vector2.right * player.RunSpeed;
            for (int step = 0; step < 55; step++) yield return null;
            Assert.That(body.position.x, Is.GreaterThan(115.2f), "The vehicle side must leave an open underpass.");
            Assert.That(vehicles[0].transform.position.x, Is.GreaterThan(origin.x + .5f));
            Assert.That(session.FacilitiesCompleted, Is.Zero);

            var firstRoof = vehicles[0].GetComponent<EdgeCollider2D>();
            Bounds roof = firstRoof.bounds;
            player.ResetAt(new Vector2(roof.center.x - .6f, roof.max.y + .9f));
            for (int step = 0; step < 35 && body.position.x < firstRoof.bounds.max.x - .55f; step++)
            {
                player.SendMessage("FixedUpdate");
                yield return null;
            }
            Assert.That(player.Grounded, Is.True, "The first roof must support takeoff.");
            Assert.That(session.FacilitiesCompleted, Is.Zero);
            SetMotorField(player, "jumpQueued", true);
            SetMotorField(player, "jumpHeld", true);
            for (int step = 0; step < 52 && session.FacilitiesCompleted == 0; step++)
            {
                player.SendMessage("FixedUpdate");
                yield return null;
            }
            Assert.That(session.FacilitiesCompleted, Is.EqualTo(1),
                "A continuous jump from the first moving roof must reach the second.");
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ChainOpensInOrderAndCountsTwoRiddenSectionsOnce()
        {
            var root = new GameObject("Chain");
            var route = root.AddComponent<RouteShaper>();
            route.Configure(new Vector2(108, 8), new Vector2(132, 9), new Vector2(132, 8),
                "연쇄 전광판", RouteShaper.Kind.Chain);
            PlayerMotor player = session.Player;
            player.RailEnabled = true;
            player.ResetAt(new Vector2(103, 8));
            Time.captureDeltaTime = Time.fixedDeltaTime;
            MagneticRail[] pieces = root.GetComponentsInChildren<MagneticRail>();
            Assert.That(route.TryShape(player, 1), Is.False);
            Assert.That(route.TryShape(player, -1), Is.True);
            Assert.That(pieces[0].enabled, Is.True);
            Assert.That(pieces[1].enabled, Is.False);
            Assert.That(pieces[2].enabled, Is.False);
            for (int step = 0; step < 11; step++) yield return null;
            Assert.That(pieces[1].enabled, Is.True);
            Assert.That(pieces[2].enabled, Is.False);
            for (int step = 0; step < 10; step++) yield return null;
            Assert.That(pieces[2].enabled, Is.True);
            Assert.That(session.FacilitiesCompleted, Is.Zero);

            player.ResetAt(new Vector2(109f, 8.7f));
            SetMotorField(player, "magnetMode", 1);
            for (int step = 0; step < 115; step++)
            {
                player.SendMessage("FixedUpdate");
                yield return null;
            }
            Assert.That(session.FacilitiesCompleted, Is.EqualTo(1), "Riding the rail gaps must link at least two pieces and reward once.");
            Assert.That(session.Deaths, Is.Zero);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator HardRelaySectionsConnectBothRingsThroughPhysics()
        {
            GameSession.Active.StartEndless(true);
            yield return null;
            yield return null;
            session = GameSession.Active;
            session.SetPaused(false);
            var player = session.Player;
            player.enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            var course = session.GetComponent<EndlessCourse>();
            var seen = new System.Collections.Generic.HashSet<string>();
            Time.captureDeltaTime = Time.fixedDeltaTime;
            for (int section = 0; section < 16 && seen.Count < 4; section++)
            {
                course.Advance(section * 32f);
                // Only exercise each generated challenge once per test.
                foreach (var root in Object.FindObjectsOfType<RelayGate>())
                {
                    var chunk = root.transform.parent;
                    if (Mathf.Abs(chunk.position.x - section * 32f) > .1f) continue;
                    string family = chunk.name.Split('/')[1].Trim();
                    if (!seen.Add(family)) continue;
                    var rings = chunk.GetComponentsInChildren<RelayGate>();
                    System.Array.Sort(rings, (a,b) => a.transform.position.x.CompareTo(b.transform.position.x));
                    var rails = chunk.GetComponentsInChildren<MagneticRail>();
                    System.Array.Sort(rails, (a,b) => a.StartPoint.x.CompareTo(b.StartPoint.x));
                    var entryRail = rails[0];
                    float startX = entryRail.StartPoint.x - (family == "Viaduct" ? 9f : .8f);
                    var street = chunk.Find("Rooftop platform").GetComponent<BoxCollider2D>();
                    player.ResetAt(new Vector2(startX, street.bounds.max.y + .455f));
                    SetMotorField(player, "magnetMode", 1);
                    SetMotorField(player, "jumpHeld", true);
                    SetMotorField(player, "jumpQueued", true);
                    for (int tick = 0; tick < 110 && !player.IsGrinding; tick++)
                    {
                        if (family == "Viaduct" && player.Grounded && body.position.x >= entryRail.StartPoint.x - 2f)
                            SetMotorField(player, "jumpQueued", true);
                        player.SendMessage("FixedUpdate");
                        yield return new WaitForFixedUpdate();
                    }
                    Assert.That(player.IsGrinding, Is.True, family + " could not enter from street level");
                    bool acted = false;
                    bool wasDashing = false;
                    float dashStart = 0f;
                    float dashEnd = 0f;
                    for (int tick = 0; tick < 150 && body.position.x < rings[1].transform.position.x + 2f; tick++)
                    {
                        if (!acted && body.position.x >= rings[0].transform.position.x + (family == "Viaduct" ? 3.5f : 0f))
                        {
                            acted = true;
                            dashStart = body.position.x;
                            if (family == "Switchback") SetMotorField(player, "jumpQueued", true);
                            else if (family == "Viaduct") SetMotorField(player, "magnetMode", -1);
                            else SetMotorField(player, "dashQueued", true);
                        }
                        player.SendMessage("FixedUpdate");
                        if (wasDashing && !player.IsDashing) dashEnd = body.position.x;
                        wasDashing = player.IsDashing;
                        yield return new WaitForFixedUpdate();
                        if (wasDashing) dashEnd = body.position.x;
                    }
                    Assert.That(acted, Is.True, family);
                    Assert.That(rings[1].gameObject.activeSelf, Is.False, family + " missed the destination ring");
                    Assert.That(session.FacilityFeedback, Is.EqualTo("환승 링 연결 +400"));
                    if (family == "Launch" || family == "Dive")
                        Assert.That(dashEnd - dashStart, Is.GreaterThan(6f), family + " at " + dashStart + " to " + dashEnd);
                }
            }
            Assert.That(seen.Count, Is.EqualTo(4));
            session.StartEndless(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessDifficultiesCrossGeneratedGapsWithJumpAndDash()
        {
            foreach (bool hard in new[] { true, false })
            {
                GameSession.Active.StartEndless(hard);
                yield return null;
                yield return null;
                session = GameSession.Active;
                session.SetPaused(false);
                Assert.That(session.IsEndless && GameSession.HardEndless == hard, Is.True);
                Assert.That(Object.FindObjectsOfType<ExitGate>(), Is.Empty);
                var player = session.Player;
                var course = session.GetComponent<EndlessCourse>();
                var body = player.GetComponent<Rigidbody2D>();
                player.enabled = false;
                Time.captureDeltaTime = Time.fixedDeltaTime;
                // Use the generated colliders at both introductory and full difficulty.
                foreach (int chunk in new[] { 0, 2, 12, 20 })
                {
                    float x = chunk * 32f;
                    course.Advance(x);
                    Physics2D.SyncTransforms();
                    var hits = Physics2D.RaycastAll(new Vector2(x + 4f, 12f), Vector2.down, 24f, LayerMask.GetMask("Ground"));
                    var takeoff = System.Array.Find(hits, hit => hit.collider.name == "Rooftop platform");
                    Assert.That(takeoff.collider, Is.Not.Null);
                    Bounds floor = takeoff.collider.bounds;
                    float edge = floor.max.x;
                    player.ResetAt(new Vector2(edge - .45f, floor.max.y + .455f));
                    player.SendMessage("FixedUpdate");
                    Assert.That(player.Grounded, Is.True);
                    SetMotorField(player, "jumpQueued", true);
                    SetMotorField(player, "jumpHeld", true);
                    bool landed = false;
                    for (int tick = 0; tick < 85 && !landed; tick++)
                    {
                        if (tick == 18) SetMotorField(player, "dashQueued", true);
                        player.SendMessage("FixedUpdate");
                        yield return null;
                        Assert.That(session.Deaths, Is.Zero, "Jump failed at chunk " + chunk);
                        landed = player.Grounded && body.position.x >= x + 34f;
                    }
                    Assert.That(landed, Is.True, "No landing beyond chunk " + chunk);
                }
            }
        }

        [UnityTest]
        public IEnumerator EndlessStreamsChunksScoresStarsAndEndsOnceWithoutRespawning()
        {
            string key = EndlessRecords.Key;
            bool existed = PlayerPrefs.HasKey(key);
            string saved = PlayerPrefs.GetString(key);
            try
            {
                SceneManager.LoadScene("Endless");
                yield return null; yield return null;
                session = GameSession.Active;
                session.SetPaused(false);
                session.Player.enabled = false;
                EndlessCourse course = session.GetComponent<EndlessCourse>();
                Assert.That(session.IsEndless, Is.True);
                Assert.That(session.FactoryWall.size.x, Is.LessThan(256f), "Streaming background must not expand to the placeholder course end.");
                Assert.That(Object.FindObjectsOfType<ExitGate>(), Is.Empty);
                Assert.That(course.LiveChunks, Is.GreaterThanOrEqualTo(4));
                session.Player.ResetAt(new Vector2(100f, 8f));
                yield return null;
                ScorePickup[] stars = Object.FindObjectsOfType<ScorePickup>();
                int before = session.EndlessScore;
                Assert.That(stars[0].Collect(session.Player), Is.True);
                Assert.That(stars[0].Collect(session.Player), Is.False);
                Assert.That(session.EndlessScore, Is.EqualTo(before + 50));
                Assert.That(stars[1].Collect(session.Player), Is.True);
                Assert.That(session.EndlessScore, Is.EqualTo(before + 110));
                session.SetPaused(true);
                Assert.That(stars[2].Collect(session.Player), Is.False);
                session.SetPaused(false);
                var billboard = new GameObject("Score billboard").AddComponent<RouteShaper>();
                billboard.Configure(new Vector2(106, 8), new Vector2(120, 11), new Vector2(120, 8),
                    "회전 광고판", RouteShaper.Kind.Billboard);
                before = session.EndlessScore;
                Assert.That(billboard.TryShape(session.Player, 1), Is.True);
                billboard.RailTravel(session.Player, 0, 3.9f);
                Assert.That(session.EndlessScore, Is.EqualTo(before), "Construction and brief contact earn no points.");
                billboard.RailTravel(session.Player, 0, 4f);
                billboard.RailTravel(session.Player, 0, 5f);
                Assert.That(session.EndlessScore, Is.EqualTo(before + 180));
                var chain = new GameObject("Score chain").AddComponent<RouteShaper>();
                chain.Configure(new Vector2(106, 8), new Vector2(128, 9), Vector2.zero,
                    "연쇄 전광판", RouteShaper.Kind.Chain);
                Assert.That(chain.TryShape(session.Player, -1), Is.True);
                session.SetPaused(true);
                chain.RailTravel(session.Player, 0, 1f);
                session.SetPaused(false);
                chain.RailTravel(session.Player, 1, 1f);
                chain.RailTravel(session.Player, 1, 1f);
                Assert.That(session.EndlessScore, Is.EqualTo(before + 180));
                chain.RailTravel(session.Player, 2, 1f);
                Assert.That(session.EndlessScore, Is.EqualTo(before + 180 + 320 + 100));

                var templates = new System.Collections.Generic.HashSet<int>();
                int last = course.LastTemplate;
                for (int x = 128; x <= 2048; x += 32)
                {
                    course.Advance(x);
                    Assert.That(course.LastTemplate, Is.Not.EqualTo(last), "Adjacent streaming sections must differ.");
                    last = course.LastTemplate;
                    templates.Add(last);
                }
                Assert.That(templates.Count, Is.EqualTo(8));
                session.Player.ResetAt(new Vector2(2048f, 8f));
                yield return null;
                Assert.That(course.LiveChunks, Is.LessThanOrEqualTo(8));
                int score = session.EndlessScore;
                session.Die(); session.Die();
                Assert.That(session.Finished, Is.True);
                Assert.That(session.Deaths, Is.EqualTo(1));
                Assert.That(session.Respawning, Is.False);
                Assert.That(session.Player.CanMove, Is.False);
                Assert.That(EndlessRecords.Load().Exists(run => run.Score == score), Is.True);
            }
            finally
            {
                if (existed) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        private static MagneticRail CreateRail()
        {
            GameObject obj = new GameObject("Test magnetic rail");
            MagneticRail rail = obj.AddComponent<MagneticRail>();
            rail.Configure(new Vector2(99f, 7.55f), new Vector2(110f, 7.55f), 1.1f);
            return rail;
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
            yield return new WaitForSeconds(0.25f);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (session.Respawning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(session.Respawning, Is.False, "Respawn did not finish within 3 real seconds.");
        }
    }
}
