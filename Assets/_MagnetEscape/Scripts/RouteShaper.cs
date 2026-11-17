using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    // The choice happens ahead of the rider; never move a path underneath them.
    public sealed class RouteShaper : MonoBehaviour
    {
        public static readonly HashSet<RouteShaper> All = new HashSet<RouteShaper>();
        [SerializeField] private Vector2 start, upper, lower;
        [SerializeField] private string routeName;
        private int queued;
        public int Choice { get; private set; }
        [field: SerializeField] public MagneticRail Rail { get; private set; }
        [field: SerializeField] public BoxCollider2D Bridge { get; private set; }
        [SerializeField] private LineRenderer upperPreview, lowerPreview;
        [SerializeField] private SpriteRenderer bridgeVisual;
        public Vector2 StartPoint => start;
        public Vector2 UpperEnd => upper;
        public Vector2 LowerEnd => lower;
        public string Prompt => routeName + "  /  Q 상층 고속 레일 · E 안전 다리";
        private void OnEnable()
        {
            All.Add(this);
            if (Rail != null) Rail.gameObject.SetActive(true);
            if (Bridge != null) Bridge.gameObject.SetActive(true);
        }
        private void Start() { if (Rail == null) Configure(start, upper, lower, routeName); }
        private void OnDisable() { All.Remove(this); if (Rail != null) Rail.gameObject.SetActive(false); if (Bridge != null) Bridge.gameObject.SetActive(false); }
        private void OnDestroy() => All.Remove(this);

        public void Configure(Vector2 first, Vector2 up, Vector2 down, string label)
        {
            start = first; upper = up; lower = down; routeName = label;
            if (Rail == null)
            {
                GameObject rail = new GameObject("Constructed upper rail"); rail.transform.SetParent(transform);
                Rail = rail.AddComponent<MagneticRail>();
                GameObject bridge = new GameObject("Constructed lower bridge"); bridge.transform.SetParent(transform);
                bridge.layer = LayerMask.NameToLayer("Ground");
                Bridge = bridge.AddComponent<BoxCollider2D>();
                bridgeVisual = bridge.AddComponent<SpriteRenderer>();
                Texture2D texture = Texture2D.whiteTexture;
                bridgeVisual.sprite = ScorePickup.Icon;
                bridgeVisual.drawMode = SpriteDrawMode.Sliced;
                bridgeVisual.color = new Color(1f, .72f, .25f); bridgeVisual.sortingOrder = 2;
                upperPreview = Preview("Q upper route", new Color(.3f, 1f, .9f, .3f));
                lowerPreview = Preview("E bridge route", new Color(1f, .72f, .25f, .3f));
            }
            Rail.Configure(start, upper, 1.1f); Rail.enabled = false;
            Vector2 delta = lower - start;
            Bridge.transform.position = (start + lower) * .5f + Vector2.down * .12f;
            Bridge.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Bridge.size = new Vector2(delta.magnitude, .24f); bridgeVisual.size = Bridge.size;
            Bridge.enabled = bridgeVisual.enabled = false;
            upperPreview.SetPosition(0, start); upperPreview.SetPosition(1, upper);
            lowerPreview.SetPosition(0, start); lowerPreview.SetPosition(1, lower);
            Choice = queued = 0;
        }
        private LineRenderer Preview(string label, Color color)
        {
            GameObject obj = new GameObject(label); obj.transform.SetParent(transform);
            LineRenderer line = obj.AddComponent<LineRenderer>(); line.sharedMaterial = StageBootstrap.ParticleMaterial;
            line.positionCount = 2; line.useWorldSpace = true; line.startWidth = line.endWidth = .06f;
            line.startColor = line.endColor = color; line.sortingOrder = 1; return line;
        }
        public bool CanShape(PlayerMotor player)
        {
            GameSession session = GameSession.Active;
            if (player == null || session == null || session.Player != player || !player.CanMove
                || session.Paused || session.Finished || session.Respawning || session.ChangingScene || Time.timeScale == 0f) return false;
            Vector2 position = player.GetComponent<Rigidbody2D>().position;
            return position.x >= start.x - 10f && position.x < start.x - 1f && Mathf.Abs(position.y - start.y) < 6f;
        }
        public bool TryShape(PlayerMotor player, int mode)
        {
            if ((mode != 1 && mode != -1) || !CanShape(player) || Choice == mode) return false;
            bool first = Choice == 0;
            Choice = mode;
            Rail.enabled = mode == 1;
            Bridge.enabled = bridgeVisual.enabled = mode == -1;
            upperPreview.enabled = mode != 1; lowerPreview.enabled = mode != -1;
            if (first) GameSession.Active.RouteBuilt();
            GameSession.Active.Feedback(start, mode == 1 ? Color.cyan : Color.yellow, GameAudio.Cue.MagnetAttract);
            return true;
        }
        private void Update()
        {
            if (!CanShape(GameSession.Active != null ? GameSession.Active.Player : null)) { queued = 0; return; }
            queued = Input.GetKey(KeyCode.E) ? -1 : Input.GetKey(KeyCode.Q) ? 1 : 0;
        }
        private void FixedUpdate()
        {
            if (queued != 0) TryShape(GameSession.Active != null ? GameSession.Active.Player : null, queued);
        }
    }
}
