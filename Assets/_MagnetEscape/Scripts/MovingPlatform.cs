using UnityEngine;

namespace MagnetEscape
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DefaultExecutionOrder(-100)]
    public sealed class MovingPlatform : MonoBehaviour
    {
        public Vector2 Travel = new Vector2(0f, 2f);
        public float Period = 4f;
        private Rigidbody2D body;
        private Vector2 origin;
        private float elapsed;
        public Vector2 CurrentVelocity { get; private set; }
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            origin = body.position;
        }
        private void FixedUpdate()
        {
            elapsed += Time.fixedDeltaTime;
            Vector2 target = origin + Travel * (0.5f - 0.5f * Mathf.Cos(elapsed * 2f * Mathf.PI / Period));
            CurrentVelocity = (target - body.position) / Time.fixedDeltaTime;
            body.MovePosition(target);
        }
    }
}
