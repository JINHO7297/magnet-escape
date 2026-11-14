using UnityEngine;

namespace MagnetEscape
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpSpeed = 13f;
        [SerializeField, Min(0.1f)] private float gravityScale = 3f;
        [SerializeField] private float dashSpeed = 19f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.75f;
        [SerializeField] private float magnetAcceleration = 34f;
        [SerializeField] private float magnetRange = 7f;
        [SerializeField, Min(1f)] private float magneticAirControl = 24f;
        [SerializeField] private bool dashEnabled = true;
        [SerializeField] private bool magnetEnabled;
        [Header("Jump responsiveness")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferDuration = 0.12f;
        [SerializeField, Range(0.1f, 1f)] private float jumpReleaseMultiplier = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float shortJumpMultiplier = 0.5f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.08f;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private readonly RaycastHit2D[] sweepHits = new RaycastHit2D[16];
        private ContactFilter2D groundFilter;
        private ContactFilter2D hazardFilter;
        private Vector2 movement;
        private Vector2 dashDirection;
        private bool jumpQueued;
        private bool dashQueued;
        private bool cutJump;
        private bool jumpHeld;
        private bool checkJumpReleaseOnResume;
        private float jumpBuffer;
        private float coyote;
        private float dashRemaining;
        private float cooldown;
        private float facing = 1f;
        private int magnetMode;
        private int audibleMagnetMode;
        private Vector2 supportVelocity;
        private MagnetAnchor targetAnchor;
        private bool magneticMomentum;
        private float previousVerticalSpeed;
        public bool CanMove { get; set; } = true;
        public bool DashEnabled { get => dashEnabled; set => dashEnabled = value; }
        public bool MagnetEnabled { get => magnetEnabled; set => magnetEnabled = value; }
        public float Cooldown => cooldown;
        public int MagnetMode => magnetMode;
        public bool IsDashing => dashRemaining > 0f;
        public bool Grounded { get; private set; }
        public float DashReadyFraction => 1f - Mathf.Clamp01(cooldown / Mathf.Max(0.001f, dashCooldown));
        public Transform MagnetTarget => targetAnchor != null ? targetAnchor.transform : null;
        public float MagnetReach => magnetRange;
        public float FacingDirection => facing;
        public float HorizontalSpeedRelativeToGround => body.velocity.x - (Grounded ? supportVelocity.x : 0f);

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
            if (!CanMove || Time.timeScale == 0f || (GameSession.Active != null && GameSession.Active.ChangingScene)) return;
            float horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float vertical = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            movement = new Vector2(horizontal, vertical);
            if (movement.x != 0f) facing = Mathf.Sign(movement.x);
            jumpQueued |= Input.GetKeyDown(KeyCode.Space);
            bool held = Input.GetKey(KeyCode.Space);
            cutJump |= Input.GetKeyUp(KeyCode.Space) || (checkJumpReleaseOnResume && !held);
            checkJumpReleaseOnResume = false;
            jumpHeld = held;
            dashQueued |= Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            magnetMode = MagnetEnabled ? (Input.GetKey(KeyCode.E) ? -1 : Input.GetKey(KeyCode.Q) ? 1 : 0) : 0;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (!CanMove || (GameSession.Active != null && GameSession.Active.ChangingScene))
            {
                body.velocity = Vector2.zero;
                return;
            }
            bool grounded = IsGrounded();
            if (grounded && !Grounded && previousVerticalSpeed - supportVelocity.y < -2f)
                GameSession.Active?.Feedback((Vector2)transform.position + Vector2.down * 0.45f,
                    new Color(0.7f, 0.8f, 0.85f), GameAudio.Cue.Land);
            Grounded = grounded;
            if (grounded) magneticMomentum = false;
            coyote = grounded ? Mathf.Max(coyoteTime, dt) : Mathf.Max(0f, coyote - dt);
            jumpBuffer = jumpQueued ? Mathf.Max(jumpBufferDuration, dt) : Mathf.Max(0f, jumpBuffer - dt);
            jumpQueued = false;
            if (dashQueued && DashEnabled && cooldown <= 0f)
            {
                dashDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : new Vector2(facing, 0f);
                dashRemaining = dashDuration;
                magneticMomentum = false;
                cooldown = dashCooldown;
                GameSession.Active?.Feedback(transform.position, Color.cyan, GameAudio.Cue.Dash);
            }
            dashQueued = false;
            if (dashRemaining > 0f)
            {
                audibleMagnetMode = 0;
                dashRemaining -= dt;
                body.gravityScale = 0f;
                Vector2 dashVelocity = dashDirection * dashSpeed;
                if (SweepHazard(dashVelocity)) return;
                body.velocity = dashVelocity;
                previousVerticalSpeed = body.velocity.y;
                cutJump = false;
                return;
            }
            body.gravityScale = gravityScale;
            Vector2 velocity = body.velocity;
            float desiredHorizontal = movement.x * moveSpeed + (grounded ? supportVelocity.x : 0f);
            velocity.x = !grounded && magneticMomentum
                ? Mathf.MoveTowards(velocity.x, desiredHorizontal, magneticAirControl * dt)
                : desiredHorizontal;
            if (grounded && velocity.y <= supportVelocity.y + 0.1f) velocity.y = supportVelocity.y;
            if (jumpBuffer > 0f && coyote > 0f)
            {
                velocity.y = (jumpHeld ? jumpSpeed : jumpSpeed * shortJumpMultiplier) + supportVelocity.y;
                jumpBuffer = 0f;
                coyote = 0f;
                if (!jumpHeld) cutJump = false;
                GameSession.Active?.Feedback(transform.position, Color.white, GameAudio.Cue.Jump);
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
            if (targetAnchor != null && magnetMode != 0)
            {
                if (audibleMagnetMode != magnetMode)
                    GameSession.Active?.PlayCue(magnetMode > 0 ? GameAudio.Cue.MagnetAttract : GameAudio.Cue.MagnetRepel);
                audibleMagnetMode = magnetMode;
                Vector2 direction = ((Vector2)targetAnchor.transform.position - body.position).normalized;
                float acceleration = magnetAcceleration * targetAnchor.ForceMultiplier;
                velocity += direction * (magnetMode * acceleration * dt);
                velocity.x = body.velocity.x + direction.x * magnetMode * acceleration * dt + movement.x * moveSpeed * dt;
                magneticMomentum = true;
            }
            else audibleMagnetMode = 0;
            velocity.x = Mathf.Clamp(velocity.x, -18f, 18f);
            velocity.y = Mathf.Clamp(velocity.y, -22f, 20f);
            if (SweepHazard(velocity)) return;
            body.velocity = velocity;
            previousVerticalSpeed = velocity.y;
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
            GameSession session = GameSession.Active;
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
            body.position = position;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = gravityScale;
            ClearInput();
            jumpBuffer = coyote = dashRemaining = cooldown = 0f;
            Grounded = false;
            targetAnchor = null;
            magneticMomentum = false;
            supportVelocity = Vector2.zero;
            facing = 1f;
            previousVerticalSpeed = 0f;
            GetComponent<PlayerVisual>()?.ResetTrail();
        }

        public void ClearInput(bool preserveJumpRelease = false)
        {
            // A release can occur while Update is suspended by pause. Preserve only
            // that held-jump state, never a queued jump, dash or magnet command.
            checkJumpReleaseOnResume = preserveJumpRelease && (checkJumpReleaseOnResume || jumpHeld || cutJump);
            movement = Vector2.zero;
            jumpQueued = dashQueued = cutJump = jumpHeld = false;
            jumpBuffer = 0f;
            magnetMode = 0;
            audibleMagnetMode = 0;
        }

        public void SetFrozen(bool frozen)
        {
            CanMove = !frozen;
            body.velocity = Vector2.zero;
            body.simulated = !frozen;
            if (frozen) ClearInput();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponent<Hazard>() != null)
            {
                GameSession.Active?.Die();
                return;
            }
            StopBlockedDash(collision);
        }

        private void OnCollisionStay2D(Collision2D collision) => StopBlockedDash(collision);

        private void StopBlockedDash(Collision2D collision)
        {
            if (!CanMove || dashRemaining <= 0f) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Vector2.Dot(collision.GetContact(i).normal, dashDirection) > -0.6f) continue;
                dashRemaining = 0f;
                body.gravityScale = gravityScale;
                // Collision response resolves velocity. Keep the existing cooldown.
                break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Hazard>() != null) GameSession.Active.Die();
            Checkpoint checkpoint = other.GetComponent<Checkpoint>();
            if (checkpoint != null) GameSession.Active.SetCheckpoint(checkpoint);
            if (other.GetComponent<ExitGate>() != null) GameSession.Active.Complete();
        }
    }
}
