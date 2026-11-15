using UnityEngine;

namespace MagnetEscape
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 7f;
        [SerializeField] private float jumpSpeed = 13f;
        [SerializeField, Min(0.1f)] private float gravityScale = 3f;
        [SerializeField] private float dashSpeed = 19f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.75f;
        [SerializeField] private float magnetAcceleration = 34f;
        [SerializeField] private float magnetRange = 7f;
        [SerializeField] private bool dashEnabled = true;
        [SerializeField] private bool magnetEnabled;
        [Header("Jump responsiveness")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferDuration = 0.12f;
        [SerializeField, Range(0.1f, 1f)] private float jumpReleaseMultiplier = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float shortJumpMultiplier = 0.5f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.08f;
        [Header("Magnetic rail")]
        [SerializeField] private bool railEnabled;
        [SerializeField, Min(1f)] private float railSpeedMultiplier = 1.35f;
        private MagneticRail grindRail;
        private float railLockout;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private readonly RaycastHit2D[] sweepHits = new RaycastHit2D[16];
        private ContactFilter2D groundFilter;
        private ContactFilter2D hazardFilter;
        private Vector2 dashDirection;
        private bool jumpQueued;
        private bool dashQueued;
        private bool cutJump;
        private bool jumpHeld;
        private bool checkJumpReleaseOnResume;
        private float jumpBuffer;
        private float coyote;
        private float dashRemaining;
        private bool dashStepActive;
        private float cooldown;
        private int magnetMode;
        private int audibleMagnetMode;
        private Vector2 supportVelocity;
        private MagnetAnchor targetAnchor;
        private float previousVerticalSpeed;
        public bool CanMove { get; set; } = true;
        public bool DashEnabled { get => dashEnabled; set => dashEnabled = value; }
        public bool MagnetEnabled { get => magnetEnabled; set => magnetEnabled = value; }
        public bool RailEnabled { get => railEnabled; set => railEnabled = value; }
        public bool IsGrinding => grindRail != null;
        public float Cooldown => cooldown;
        public int MagnetMode => magnetMode;
        public bool IsDashing => dashStepActive;
        public bool Grounded { get; private set; }
        public float DashReadyFraction => 1f - Mathf.Clamp01(cooldown / Mathf.Max(0.001f, dashCooldown));
        public Transform MagnetTarget => targetAnchor != null ? targetAnchor.transform : null;
        public float MagnetReach => magnetRange;
        public float RunSpeed => Mathf.Max(0.1f, moveSpeed);
        public float FacingDirection => 1f;
        public float HorizontalSpeedRelativeToGround => body.velocity.x - (Grounded ? supportVelocity.x : 0f);
        private GameSession Session
        {
            get
            {
                GameSession session = GameSession.Active;
                return session != null && session.Player == this ? session : null;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            body.gravityScale = gravityScale;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            groundFilter = new ContactFilter2D();
            groundFilter.useTriggers = false;
            groundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            hazardFilter = new ContactFilter2D();
            hazardFilter.useTriggers = true;
            hazardFilter.SetLayerMask(~0);
        }

        private void Update()
        {
            if (!CanMove || (GameSession.Active != null && GameSession.Active.ChangingScene)) return;
            // Held levels still update during pause; queued jump/dash edges do not.
            // A menu can resume after Update, before the next physics step.
            magnetMode = (MagnetEnabled || RailEnabled) ? (Input.GetKey(KeyCode.E) ? -1 : Input.GetKey(KeyCode.Q) ? 1 : 0) : 0;
            if (Time.timeScale == 0f) return;
            jumpQueued |= Input.GetKeyDown(KeyCode.Space);
            bool held = Input.GetKey(KeyCode.Space);
            cutJump |= Input.GetKeyUp(KeyCode.Space) || (checkJumpReleaseOnResume && !held);
            checkJumpReleaseOnResume = false;
            jumpHeld = held;
            dashQueued |= Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            cooldown = Mathf.Max(0f, cooldown - dt);
            railLockout = Mathf.Max(0f, railLockout - dt);
            if (!CanMove || (GameSession.Active != null && GameSession.Active.ChangingScene))
            {
                ReleaseRail();
                body.velocity = Vector2.zero;
                body.gravityScale = gravityScale;
                dashRemaining = 0f;
                dashStepActive = false;
                return;
            }
            bool grounded = IsGrounded();
            if (grounded && !Grounded && previousVerticalSpeed - supportVelocity.y < -2f)
                Session?.Feedback((Vector2)transform.position + Vector2.down * 0.45f,
                    new Color(0.7f, 0.8f, 0.85f), GameAudio.Cue.Land);
            Grounded = grounded;
            coyote = grounded ? Mathf.Max(coyoteTime, dt) : Mathf.Max(0f, coyote - dt);
            jumpBuffer = jumpQueued ? Mathf.Max(jumpBufferDuration, dt) : Mathf.Max(0f, jumpBuffer - dt);
            jumpQueued = false;
            if (dashQueued && DashEnabled && cooldown <= 0f)
            {
                ReleaseRail();
                dashDirection = Vector2.right;
                dashRemaining = dashDuration;
                cooldown = dashCooldown;
                Session?.Feedback(transform.position, Color.cyan, GameAudio.Cue.Dash);
            }
            dashQueued = false;
            // Keep the public state active for the whole step that applies dash
            // velocity, including the final step after its timer reaches zero.
            dashStepActive = dashRemaining > 0f;
            if (dashStepActive)
            {
                audibleMagnetMode = 0;
                dashRemaining = Mathf.Max(0f, dashRemaining - dt);
                body.gravityScale = 0f;
                Vector2 dashVelocity = dashDirection * Mathf.Max(RunSpeed, dashSpeed);
                if (SweepHazard(dashVelocity)) return;
                body.velocity = dashVelocity;
                previousVerticalSpeed = body.velocity.y;
                cutJump = false;
                return;
            }
            bool railLaunch;
            if (StepRail(out railLaunch)) return;
            body.gravityScale = gravityScale;
            Vector2 velocity = body.velocity;
            velocity.x = RunSpeed;
            if (grounded && velocity.y <= supportVelocity.y + 0.1f) velocity.y = supportVelocity.y;
            if (railLaunch || (jumpBuffer > 0f && coyote > 0f))
            {
                velocity.y = (railLaunch || jumpHeld ? jumpSpeed : jumpSpeed * shortJumpMultiplier) + supportVelocity.y;
                jumpBuffer = 0f;
                coyote = 0f;
                if (!jumpHeld) cutJump = false;
                Session?.Feedback(transform.position, Color.white, GameAudio.Cue.Jump);
            }
            if (cutJump && velocity.y > 0f) velocity.y *= jumpReleaseMultiplier;
            cutJump = false;
            bool keepTarget = MagnetEnabled && magnetMode != 0 && targetAnchor != null
                && targetAnchor.isActiveAndEnabled && targetAnchor.Contains(body.position, magnetRange);
            if (!keepTarget)
            {
                targetAnchor = null;
                float distance = float.MaxValue;
                if (MagnetEnabled)
                {
                    foreach (MagnetAnchor anchor in MagnetAnchor.All)
                    {
                        float candidate = Vector2.Distance(body.position, anchor.transform.position);
                        if (candidate < distance && anchor.Contains(body.position, magnetRange))
                        {
                            targetAnchor = anchor;
                            distance = candidate;
                        }
                    }
                }
            }
            if (targetAnchor != null && magnetMode != 0 && !grounded)
            {
                if (audibleMagnetMode != magnetMode)
                    Session?.PlayCue(magnetMode > 0 ? GameAudio.Cue.MagnetAttract : GameAudio.Cue.MagnetRepel);
                audibleMagnetMode = magnetMode;
                Vector2 direction = ((Vector2)targetAnchor.transform.position - body.position).normalized;
                float acceleration = magnetAcceleration * targetAnchor.ForceMultiplier;
                // The board maintains forward propulsion. Magnetism changes only
                // the vertical flight path; it cannot stop or reverse the runner.
                velocity.y += direction.y * magnetMode * acceleration * dt;
            }
            else audibleMagnetMode = 0;
            velocity.x = RunSpeed;
            velocity.y = Mathf.Clamp(velocity.y, -22f, 20f);
            if (SweepHazard(velocity)) return;
            body.velocity = velocity;
            previousVerticalSpeed = velocity.y;
        }

        private bool StepRail(out bool launch)
        {
            launch = false;
            Bounds bounds = shape.bounds;
            Vector2 board = new Vector2(bounds.center.x, bounds.min.y);
            if (grindRail != null)
            {
                launch = jumpBuffer > 0f || magnetMode < 0;
                if (!RailEnabled || !grindRail.Covers(board.x) || magnetMode != 1 || launch)
                    ReleaseRail();
            }
            if (grindRail == null && !launch && RailEnabled && !Grounded && magnetMode == 1 && railLockout <= 0f)
            {
                float nearest = float.PositiveInfinity;
                foreach (MagneticRail rail in MagneticRail.All)
                {
                    if (!rail.CanCatch(bounds.center, bounds.extents.y)) continue;
                    float distance = Mathf.Abs(board.y - rail.HeightAt(board.x));
                    if (distance >= nearest) continue;
                    grindRail = rail;
                    nearest = distance;
                }
                if (grindRail != null)
                    Session?.Feedback(body.position, Color.cyan, GameAudio.Cue.MagnetAttract);
            }
            if (grindRail == null) return false;
            body.gravityScale = 0f;
            float speed = RunSpeed * Mathf.Max(1f, railSpeedMultiplier);
            Vector2 velocity = new Vector2(speed, grindRail.Slope * speed);
            Vector2 correction = Vector2.up * (grindRail.HeightAt(board.x) - board.y);
            if (!RailEntryClear(correction))
            {
                ReleaseRail();
                return !CanMove;
            }
            body.position += correction;
            // Check the forward route from the corrected collider pose too.
            if (SweepHazard(velocity)) return true;
            body.velocity = velocity;
            previousVerticalSpeed = velocity.y;
            coyote = 0f;
            cutJump = false;
            return true;
        }

        private bool RailEntryClear(Vector2 motion)
        {
            float distance = motion.magnitude;
            if (distance < 0.001f) return true;
            Vector2 direction = motion / distance;
            int count = shape.Cast(direction, hazardFilter, sweepHits, distance);
            float blocking = float.PositiveInfinity;
            float danger = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D other = sweepHits[i].collider;
                if (other == null) continue;
                if (other.GetComponent<Hazard>() != null)
                    danger = Mathf.Min(danger, sweepHits[i].distance);
                else if (!other.isTrigger && Vector2.Dot(sweepHits[i].normal, direction) < -0.5f)
                    blocking = Mathf.Min(blocking, sweepHits[i].distance);
            }
            if (danger <= Mathf.Min(distance, blocking) + 0.001f)
            {
                Session?.Die();
                return false;
            }
            return blocking > distance + 0.001f;
        }

        private void ReleaseRail()
        {
            if (grindRail != null) railLockout = 0.2f;
            grindRail = null;
            body.gravityScale = gravityScale;
        }

        private bool IsGrounded()
        {
            supportVelocity = Vector2.zero;
            int count = shape.Cast(Vector2.down, groundFilter, hits, groundProbeDistance);
            for (int i = 0; i < count; i++)
                if (hits[i].normal.y > 0.6f)
                {
                    if (hits[i].rigidbody != null)
                    {
                        MovingPlatform platform = hits[i].rigidbody.GetComponent<MovingPlatform>();
                        supportVelocity = platform != null ? platform.CurrentVelocity : hits[i].rigidbody.velocity;
                    }
                    return true;
                }
            return false;
        }

        private bool SweepHazard(Vector2 velocity)
        {
            GameSession session = Session;
            if (session == null || session.Paused || session.Respawning || session.Finished) return false;
            // Include this step's gravity. Collider casts supplement trigger callbacks
            // for fast movement; they do not activate checkpoints or exits early.
            Vector2 motion = (velocity + Physics2D.gravity * body.gravityScale * Time.fixedDeltaTime) * Time.fixedDeltaTime;
            float distance = motion.magnitude;
            if (distance < 0.001f) return false;
            Vector2 direction = motion / distance;
            int count = shape.Cast(direction, hazardFilter, sweepHits, distance);
            float hazardDistance = float.PositiveInfinity;
            float blockingDistance = distance;
            for (int i = 0; i < count; i++)
            {
                Collider2D other = sweepHits[i].collider;
                if (other == null) continue;
                if (other.GetComponent<Hazard>() != null)
                {
                    hazardDistance = Mathf.Min(hazardDistance, sweepHits[i].distance);
                    continue;
                }
                // A facing wall can block the route to a hazard. Ignore grazing
                // floor contacts so standing beside a spike still checks forward.
                if (!other.isTrigger && Vector2.Dot(sweepHits[i].normal, direction) < -0.5f)
                    blockingDistance = Mathf.Min(blockingDistance, sweepHits[i].distance);
            }
            if (hazardDistance > blockingDistance + 0.001f) return false;
            session.Die();
            return true;
        }

        public void ResetAt(Vector2 position)
        {
            ReleaseRail();
            railLockout = 0f;
            // While simulated is false Unity rebuilds the body from its Transform
            // when re-enabled. Move both so respawning cannot restore the death
            // location, and the camera/trail see the new spawn in this frame.
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            body.position = position;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = gravityScale;
            ClearInput();
            jumpBuffer = coyote = dashRemaining = cooldown = 0f;
            dashStepActive = false;
            Grounded = false;
            targetAnchor = null;
            supportVelocity = Vector2.zero;
            previousVerticalSpeed = 0f;
            GetComponent<PlayerVisual>()?.ResetTrail();
        }

        public void ClearInput(bool preserveJumpRelease = false)
        {
            // Preserve pending jump release and held Q/E levels through pause,
            // while always discarding queued jump and dash edges.
            checkJumpReleaseOnResume = preserveJumpRelease && (checkJumpReleaseOnResume || jumpHeld || cutJump);
            jumpQueued = dashQueued = cutJump = jumpHeld = false;
            jumpBuffer = 0f;
            // Preserve the current Q/E level across pause/resume. Paused Update
            // refreshes it, so releasing Q while paused still detaches on resume.
            if (!preserveJumpRelease) magnetMode = 0;
            audibleMagnetMode = 0;
        }

        public void SetFrozen(bool frozen)
        {
            CanMove = !frozen;
            body.velocity = Vector2.zero;
            body.simulated = !frozen;
            if (frozen)
            {
                ReleaseRail();
                dashRemaining = 0f;
                dashStepActive = false;
                body.gravityScale = gravityScale;
                ClearInput();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponent<Hazard>() != null)
            {
                Session?.Die();
                return;
            }
            StopBlockedDash(collision);
        }

        private void OnCollisionStay2D(Collision2D collision) => StopBlockedDash(collision);

        private void StopBlockedDash(Collision2D collision)
        {
            if (!CanMove || !IsDashing) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Vector2.Dot(collision.GetContact(i).normal, dashDirection) > -0.6f) continue;
                dashRemaining = 0f;
                dashStepActive = false;
                body.gravityScale = gravityScale;
                // Collision response resolves velocity. Keep the existing cooldown.
                break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            GameSession session = Session;
            if (session == null) return;
            if (other.GetComponent<Hazard>() != null)
            {
                session.Die();
                return;
            }
            Checkpoint checkpoint = other.GetComponent<Checkpoint>();
            if (checkpoint != null) session.SetCheckpoint(checkpoint);
            if (other.GetComponent<ExitGate>() != null) session.Complete();
        }
    }
}
