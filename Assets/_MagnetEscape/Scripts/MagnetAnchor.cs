using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    public sealed class MagnetAnchor : MonoBehaviour
    {
        public static readonly HashSet<MagnetAnchor> All = new HashSet<MagnetAnchor>();
        [SerializeField, Min(0.5f)] private float range = 7f;
        [SerializeField, Range(0.1f, 3f)] private float forceMultiplier = 1f;
        private LineRenderer ring;
        private SpriteRenderer visual;
        private float drawnRadius = -1f;
        public float ForceMultiplier => forceMultiplier;
        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);
        private void Start() => InitializeRing();

        public void Configure(float radius, float strength)
        {
            range = Mathf.Max(0.5f, radius);
            forceMultiplier = Mathf.Clamp(strength, 0.1f, 3f);
            InitializeRing();
        }

        public bool Contains(Vector2 point, float playerReach)
        {
            float radius = Mathf.Min(range, playerReach);
            return ((Vector2)transform.position - point).sqrMagnitude <= radius * radius;
        }

        private void InitializeRing()
        {
            ring = GetComponent<LineRenderer>();
            if (ring == null) ring = gameObject.AddComponent<LineRenderer>();
            visual = GetComponent<SpriteRenderer>();
            ring.sharedMaterial = StageBootstrap.ParticleMaterial;
            ring.useWorldSpace = false;
            ring.positionCount = 64;
            ring.loop = true;
            ring.sortingOrder = -2;
            ring.startWidth = ring.endWidth = 0.025f;
            ring.startColor = ring.endColor = new Color(0.4f, 0.65f, 0.65f, 0.14f);
            DrawRing(range);
        }

        private void DrawRing(float radius)
        {
            drawnRadius = radius;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0.1f));
            }
        }

        private void Update()
        {
            if (ring == null) InitializeRing();
            PlayerMotor player = GameSession.Active != null ? GameSession.Active.Player : null;
            float radius = player != null ? Mathf.Min(range, player.MagnetReach) : range;
            if (!Mathf.Approximately(radius, drawnRadius)) DrawRing(radius);
            bool selected = player != null && player.MagnetEnabled && player.MagnetTarget == transform;
            bool active = selected && player.CanMove && player.MagnetMode != 0 && !player.IsDashing;
            Color color = active && player.MagnetMode < 0 ? new Color(1f, 0.75f, 0.3f) : new Color(0.35f, 0.95f, 0.85f);
            color.a = active ? 0.65f : selected ? 0.35f : 0.12f;
            ring.startColor = ring.endColor = color;
            ring.startWidth = ring.endWidth = active ? 0.05f : 0.025f;
            if (visual != null) visual.color = selected ? Color.Lerp(Color.white, color, 0.35f) : Color.white;
        }
    }
}
