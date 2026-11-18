using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    // A finite, authored grind route. It never captures without the player's Q input.
    [RequireComponent(typeof(LineRenderer))]
    public sealed class MagneticRail : MonoBehaviour
    {
        public static readonly HashSet<MagneticRail> All = new HashSet<MagneticRail>();
        [SerializeField] private Vector2 start;
        [SerializeField] private Vector2 end;
        [SerializeField, Min(0.1f)] private float catchRange = 1.1f;
        [field: SerializeField] public bool Overhead { get; private set; }
        [field: SerializeField] public RouteShaper Route { get; private set; }
        [field: SerializeField] public int Piece { get; private set; }
        private LineRenderer line;
        public Vector2 StartPoint => start;
        public Vector2 EndPoint => end;
        public float Slope => (end.y - start.y) / Mathf.Max(0.01f, end.x - start.x);

        private void OnEnable()
        {
            All.Add(this);
            if (line != null) line.enabled = true;
        }

        private void OnDisable()
        {
            All.Remove(this);
            if (line != null) line.enabled = false;
        }

        private void Start() => Draw();

        public void Configure(Vector2 first, Vector2 last, float reach, bool overhead = false, RouteShaper route = null, int piece = 0)
        {
            Overhead = overhead;
            Route = route;
            Piece = piece;
            start = first;
            end = last;
            catchRange = Mathf.Max(0.1f, reach);
            Draw();
        }

        public bool Covers(float x) => isActiveAndEnabled && end.x > start.x + 0.1f && x >= start.x - 0.15f && x <= end.x;
        public float HeightAt(float x) => Mathf.Lerp(start.y, end.y, Mathf.InverseLerp(start.x, end.x, x));
        public float ContactY(Vector2 center, float halfHeight) => center.y + (Overhead ? halfHeight : -halfHeight);
        public bool CanCatch(Vector2 center, float halfHeight)
        {
            if (!Covers(center.x)) return false;
            float delta = HeightAt(center.x) - ContactY(center, halfHeight);
            return Mathf.Abs(delta) <= catchRange && (!Overhead || delta >= -.15f);
        }

        private void Draw()
        {
            line = GetComponent<LineRenderer>();
            line.sharedMaterial = StageBootstrap.ParticleMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = line.endWidth = Overhead ? .19f : .12f;
            line.startColor = Overhead ? new Color(.75f, .6f, 1f) : new Color(.3f, 1f, .9f);
            line.endColor = new Color(0.9f, 0.5f, 1f);
            line.sortingOrder = 4;
            line.enabled = isActiveAndEnabled;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, catchRange);
            Gizmos.DrawWireSphere(end, catchRange);
        }
    }
}
