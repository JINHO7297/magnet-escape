using UnityEngine;

namespace MagnetEscape
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DefaultExecutionOrder(-100)]
    public sealed class MovingPlatform : MonoBehaviour
    {
        public Vector2 Travel = new Vector2(0f, 2f);
        [Min(0.1f)] public float Period = 4f;
        private Rigidbody2D body;
        private Vector2 origin;
        private float elapsed;
        private LineRenderer rail;
        private Vector2 drawnTravel;
        public Vector2 CurrentVelocity { get; private set; }
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            origin = body.position;
            DrawRoute(origin);
        }

        public void Configure(Vector2 travel, float period)
        {
            Travel = travel;
            Period = Mathf.Max(0.1f, period);
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            DrawRoute(Application.isPlaying ? origin : (Vector2)transform.position);
        }

        private void DrawRoute(Vector2 start)
        {
            rail = GetComponent<LineRenderer>();
            if (rail == null) rail = gameObject.AddComponent<LineRenderer>();
            rail.sharedMaterial = StageBootstrap.ParticleMaterial;
            rail.useWorldSpace = true;
            rail.positionCount = 2;
            rail.startWidth = rail.endWidth = 0.035f;
            rail.startColor = rail.endColor = new Color(1f, 0.75f, 0.25f, 0.3f);
            rail.sortingOrder = -2;
            rail.SetPosition(0, start);
            rail.SetPosition(1, start + Travel);
            drawnTravel = Travel;
        }
        private void FixedUpdate()
        {
            elapsed += Time.fixedDeltaTime;
            if (drawnTravel != Travel) DrawRoute(origin);
            Vector2 target = origin + Travel * (0.5f - 0.5f * Mathf.Cos(elapsed * 2f * Mathf.PI / Mathf.Max(0.1f, Period)));
            CurrentVelocity = (target - body.position) / Time.fixedDeltaTime;
            body.MovePosition(target);
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 start = Application.isPlaying ? origin : (Vector2)transform.position;
            Gizmos.color = new Color(1f, 0.75f, 0.25f);
            Gizmos.DrawLine(start, start + Travel);
            Gizmos.DrawWireSphere(start, 0.12f);
            Gizmos.DrawWireSphere(start + Travel, 0.12f);
        }
    }
}
