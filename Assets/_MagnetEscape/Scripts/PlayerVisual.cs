using UnityEngine;

namespace MagnetEscape
{
    // Placeholder animation only touches the renderer; the physics shape stays stable.
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    public sealed class PlayerVisual : MonoBehaviour
    {
        private SpriteRenderer visual;
        private Rigidbody2D body;
        private PlayerMotor motor;
        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            motor = GetComponent<PlayerMotor>();
        }
        private void Update()
        {
            float stride = Mathf.Abs(body.velocity.x) > 0.2f ? Mathf.Sin(Time.time * 18f) * 0.06f : 0f;
            visual.size = new Vector2(0.7f + stride, 0.9f - stride);
            visual.color = !motor.CanMove ? Color.white : motor.MagnetMode > 0 ? Color.magenta : motor.MagnetMode < 0 ? Color.yellow : Color.cyan;
        }
    }
}
