using UnityEngine;

namespace MagnetEscape
{
    // Sprite animation changes appearance only; the physics shape stays stable.
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    public sealed class PlayerVisual : MonoBehaviour
    {
        private SpriteRenderer visual;
        private Rigidbody2D body;
        private PlayerMotor motor;
        private TrailRenderer trail;
        private LineRenderer tether;
        private readonly Sprite[] run = new Sprite[4];
        private Sprite idle;
        private Sprite jump;
        private Sprite fall;
        private Sprite dash;
        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            motor = GetComponent<PlayerMotor>();
            for (int i = 0; i < run.Length; i++) run[i] = GameArt.Get("robot_run_" + i);
            idle = GameArt.Get("robot_idle");
            jump = GameArt.Get("robot_jump");
            fall = GameArt.Get("robot_fall");
            dash = GameArt.Get("robot_dash");
            visual.drawMode = SpriteDrawMode.Simple;
            visual.sortingOrder = 10;
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = StageBootstrap.ParticleMaterial;
            trail.time = 0.15f;
            trail.startWidth = 0.45f;
            trail.endWidth = 0f;
            trail.startColor = new Color(0.35f, 1f, 0.85f, 0.7f);
            trail.endColor = new Color(0.35f, 1f, 0.85f, 0f);
            trail.sortingOrder = 8;
            trail.emitting = false;
            tether = gameObject.AddComponent<LineRenderer>();
            tether.sharedMaterial = StageBootstrap.ParticleMaterial;
            tether.useWorldSpace = true;
            tether.startWidth = 0.045f;
            tether.endWidth = 0.02f;
            tether.positionCount = 2;
            tether.sortingOrder = 6;
            tether.enabled = false;
        }
        private void Update()
        {
            if (Time.timeScale == 0f) return;
            visual.enabled = GameSession.Active == null || !GameSession.Active.Respawning;
            if (Mathf.Abs(body.velocity.x) > 0.2f) visual.flipX = body.velocity.x < 0f;
            visual.sprite = motor.IsDashing ? dash : !motor.Grounded ? (body.velocity.y > 0f ? jump : fall)
                : Mathf.Abs(body.velocity.x) > 0.2f ? run[Mathf.FloorToInt(Time.time * 12f) % run.Length] : idle;
            visual.color = motor.MagnetMode > 0 ? new Color(1f, 0.7f, 0.9f) : motor.MagnetMode < 0 ? new Color(1f, 0.9f, 0.55f) : Color.white;
            trail.emitting = motor.CanMove && motor.IsDashing;
            if (!motor.CanMove) trail.Clear();
            tether.enabled = motor.CanMove && !motor.IsDashing && motor.MagnetMode != 0 && motor.MagnetTarget != null;
            if (tether.enabled)
            {
                tether.SetPosition(0, transform.position);
                tether.SetPosition(1, motor.MagnetTarget.position);
                tether.startColor = tether.endColor = motor.MagnetMode > 0 ? Color.cyan : new Color(1f, 0.75f, 0.3f);
            }
        }

        public void ResetTrail()
        {
            trail.Clear();
            tether.enabled = false;
            visual.flipX = false;
        }
    }
}
