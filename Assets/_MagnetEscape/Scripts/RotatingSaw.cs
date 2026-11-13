using UnityEngine;
namespace MagnetEscape
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RotatingSaw : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = -180f;
        [SerializeField] private Vector2 travel;
        [SerializeField, Min(0.5f)] private float period = 3.5f;
        [SerializeField] private float phase;
        private Rigidbody2D body;
        private Vector2 origin;
        private float elapsed;
        private LineRenderer rail;
        private Vector2 drawnTravel;
        public Vector2 Travel => travel;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            origin = body.position;
            DrawRoute(origin);
        }

        public void Configure(Vector2 displacement, float seconds, float startPhase)
        {
            travel = displacement;
            period = Mathf.Max(0.5f, seconds);
            phase = startPhase;
            Rigidbody2D configuredBody = GetComponent<Rigidbody2D>();
            if (configuredBody == null) configuredBody = gameObject.AddComponent<Rigidbody2D>();
            configuredBody.bodyType = RigidbodyType2D.Kinematic;
            DrawRoute(Application.isPlaying ? origin : (Vector2)transform.position);
        }

        private void DrawRoute(Vector2 start)
        {
            rail = GetComponent<LineRenderer>();
            if (rail == null) rail = gameObject.AddComponent<LineRenderer>();
            rail.sharedMaterial = StageBootstrap.ParticleMaterial;
            rail.useWorldSpace = true;
            rail.positionCount = 2;
            rail.startWidth = rail.endWidth = 0.04f;
            rail.startColor = rail.endColor = new Color(1f, 0.45f, 0.3f, 0.4f);
            rail.sortingOrder = -1;
            rail.SetPosition(0, start - travel);
            rail.SetPosition(1, start + travel);
            rail.enabled = travel.sqrMagnitude >= 0.001f;
            drawnTravel = travel;
        }

        private void FixedUpdate()
        {
            elapsed += Time.fixedDeltaTime;
            if (drawnTravel != travel) DrawRoute(origin);
            float cycle = elapsed * Mathf.PI * 2f / Mathf.Max(0.5f, period) + phase;
            body.MovePosition(origin + travel * Mathf.Sin(cycle));
            body.MoveRotation(body.rotation + rotationSpeed * Time.fixedDeltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 center = Application.isPlaying ? origin : (Vector2)transform.position;
            Gizmos.color = new Color(1f, 0.45f, 0.3f);
            Gizmos.DrawLine(center - travel, center + travel);
            Gizmos.DrawWireSphere(center - travel, 0.45f);
            Gizmos.DrawWireSphere(center + travel, 0.45f);
        }
    }
}
