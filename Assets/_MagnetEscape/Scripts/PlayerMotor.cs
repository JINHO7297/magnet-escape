using UnityEngine;

namespace MagnetEscape
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpSpeed = 13f;
        [SerializeField] private float dashSpeed = 19f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.75f;
        [SerializeField] private float magnetAcceleration = 34f;
        [SerializeField] private float magnetRange = 7f;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private ContactFilter2D groundFilter;
        private Vector2 movement;
        private Vector2 dashDirection;
        private bool jumpQueued;
        private bool dashQueued;
        private bool cutJump;
        private bool jumpHeld;
        private float jumpBuffer;
        private float coyote;
        private float dashRemaining;
        private float cooldown;
        private float facing = 1f;
        private int magnetMode;
        private Vector2 supportVelocity;
        private MagnetAnchor targetAnchor;
        public bool CanMove { get; set; } = true;
        public bool DashEnabled { get; set; } = true;
        public bool MagnetEnabled { get; set; }
        public float Cooldown => cooldown;
        public int MagnetMode => magnetMode;
        public bool IsDashing => dashRemaining > 0f;
        public bool Grounded { get; private set; }
        public float DashReadyFraction => 1f - Mathf.Clamp01(cooldown / dashCooldown);
        public Transform MagnetTarget => targetAnchor != null ? targetAnchor.transform : null;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            groundFilter = new ContactFilter2D();
            groundFilter.useTriggers = false;
            groundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
        }

        private void Update()
        {
            if (!CanMove || Time.timeScale == 0f) return;
            float horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float vertical = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            movement = new Vector2(horizontal, vertical);
            if (movement.x != 0f) facing = Mathf.Sign(movement.x);
            jumpQueued |= Input.GetKeyDown(KeyCode.Space);
            cutJump |= Input.GetKeyUp(KeyCode.Space);
            jumpHeld = Input.GetKey(KeyCode.Space);
            dashQueued |= Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            magnetMode = MagnetEnabled ? (Input.GetKey(KeyCode.E) ? -1 : Input.GetKey(KeyCode.Q) ? 1 : 0) : 0;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            cooldown = Mathf.Max(0f, cooldown - dt);
            if (!CanMove)
            {
                body.velocity = Vector2.zero;
                return;
            }
            bool grounded = IsGrounded();
            Grounded = grounded;
            coyote = grounded ? 0.1f : Mathf.Max(0f, coyote - dt);
            jumpBuffer = jumpQueued ? 0.12f : Mathf.Max(0f, jumpBuffer - dt);
            jumpQueued = false;
            if (dashQueued && DashEnabled && cooldown <= 0f)
            {
                dashDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : new Vector2(facing, 0f);
                dashRemaining = dashDuration;
                cooldown = dashCooldown;
                GameSession.Active?.Feedback(transform.position, Color.cyan, 700f);
            }
            dashQueued = false;
            if (dashRemaining > 0f)
            {
                dashRemaining -= dt;
                body.gravityScale = 0f;
                body.velocity = dashDirection * dashSpeed;
                cutJump = false;
                return;
            }
            body.gravityScale = 3f;
            Vector2 velocity = body.velocity;
            velocity.x = movement.x * moveSpeed + (grounded ? supportVelocity.x : 0f);
            if (grounded && velocity.y <= supportVelocity.y + 0.1f) velocity.y = supportVelocity.y;
            if (jumpBuffer > 0f && coyote > 0f)
            {
                velocity.y = (jumpHeld ? jumpSpeed : jumpSpeed * 0.5f) + supportVelocity.y;
                jumpBuffer = 0f;
                coyote = 0f;
                if (!jumpHeld) cutJump = false;
                GameSession.Active?.Feedback(transform.position, Color.white, 500f);
            }
            if (cutJump && velocity.y > 0f) velocity.y *= 0.45f;
            cutJump = false;
            targetAnchor = null;
            float distance = magnetRange;
            if (MagnetEnabled)
            {
                foreach (MagnetAnchor anchor in MagnetAnchor.All)
                {
                    float candidate = Vector2.Distance(body.position, anchor.transform.position);
                    if (candidate < distance) { targetAnchor = anchor; distance = candidate; }
                }
                if (targetAnchor != null && magnetMode != 0)
                {
                    Vector2 direction = ((Vector2)targetAnchor.transform.position - body.position).normalized;
                    velocity += direction * (magnetMode * magnetAcceleration * dt);
                    // Preserve horizontal momentum while using magnet controls.
                    velocity.x = body.velocity.x + direction.x * magnetMode * magnetAcceleration * dt + movement.x * moveSpeed * dt;
                }
            }
            velocity.x = Mathf.Clamp(velocity.x, -18f, 18f);
            velocity.y = Mathf.Clamp(velocity.y, -22f, 20f);
            body.velocity = velocity;
        }

        private bool IsGrounded()
        {
            supportVelocity = Vector2.zero;
            int count = shape.Cast(Vector2.down, groundFilter, hits, 0.08f);
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

        public void ResetAt(Vector2 position)
        {
            body.position = position;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 3f;
            ClearInput();
            jumpBuffer = coyote = dashRemaining = cooldown = 0f;
            Grounded = false;
            targetAnchor = null;
            GetComponent<PlayerVisual>()?.ResetTrail();
        }

        public void ClearInput()
        {
            movement = Vector2.zero;
            jumpQueued = dashQueued = cutJump = jumpHeld = false;
            jumpBuffer = 0f;
            magnetMode = 0;
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
            if (collision.collider.GetComponent<Hazard>() != null) GameSession.Active.Die();
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
