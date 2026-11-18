using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    // Construction controls act on the approach. Contact, not construction, earns points.
    public sealed class RouteShaper : MonoBehaviour
    {
        public enum Kind { Switch, Billboard, Ceiling, Convoy, Chain }
        public static readonly HashSet<RouteShaper> All = new HashSet<RouteShaper>();
        [SerializeField] private Vector2 start, upper, lower;
        [SerializeField] private string routeName;
        [SerializeField] private Kind kind;
        [SerializeField] private LineRenderer upperPreview, lowerPreview;
        [SerializeField] private SpriteRenderer bridgeVisual, billboardFace;
        [SerializeField] private MagneticRail[] chain;
        [SerializeField] private LineRenderer[] chainPreviews;

        private int queued, lastPiece = -1, distinctPieces, contactDeaths;
        private float openTime;
        private bool rewarded;
        public int Choice { get; private set; }
        public float Charge { get; private set; }
        public Kind Facility => kind;
        [field: SerializeField] public MagneticRail Rail { get; private set; }
        [field: SerializeField] public BoxCollider2D Bridge { get; private set; }
        [field: SerializeField] private bool Prepared { get; set; }
        public Vector2 StartPoint => start;
        public Vector2 UpperEnd => upper;
        public Vector2 LowerEnd => lower;
        public string Prompt
        {
            get
            {
                switch (kind)
                {
                    case Kind.Billboard: return routeName + $"  /  Q 유지 각도 {Charge * 100f:F0}% · E 낮은 다리";
                    case Kind.Ceiling: return routeName + "  /  Space 점프 + Q 매달리기 · E 낙하";
                    case Kind.Convoy: return routeName + "  /  Space로 지붕 두 곳에 착지";
                    case Kind.Chain: return routeName + (Choice != 0 ? "  /  Space + Q로 열린 레일 잇기" : "  /  E로 전광판 열기 → Space + Q");
                    default: return routeName + "  /  Q 상층 고속 레일 · E 안전 다리";
                }
            }
        }
        private void OnEnable()
        {
            All.Add(this);
            foreach (Transform child in transform) child.gameObject.SetActive(true);
        }
        private void Start()
        {
            if (!Prepared) Configure(start, upper, lower, routeName, kind);
        }
        private void OnDisable()
        {
            All.Remove(this);
            if (gameObject.activeInHierarchy)
                foreach (Transform child in transform) child.gameObject.SetActive(false);
        }

        public void Configure(Vector2 first, Vector2 up, Vector2 down, string label, Kind facility = Kind.Switch)
        {
            start = first;
            upper = up;
            lower = down;
            routeName = label;
            kind = facility;
            Prepared = true;

            switch (kind)
            {
                case Kind.Convoy:
                    BuildConvoy();
                    break;
                case Kind.Chain:
                    BuildChain();
                    break;
                case Kind.Ceiling:
                    Vector2 ceilingStart = new Vector2(start.x, upper.y - 0.25f);
                    Rail = CreateRail(ceilingStart, upper, 0, true, .85f);
                    Rail.enabled = true;
                    var cable = new Color(.75f, .6f, 1f, .65f);
                    foreach (Vector2 end in new[] { ceilingStart, upper })
                        Panel("케이블 고정대", end + Vector2.up * .25f, new Vector2(.28f, .56f), cable);
                    break;
                default:
                    Rail = CreateRail(start, upper, 0);
                    BuildBridge();
                    upperPreview = Preview("Q 상층 경로", start, upper, new Color(.3f, 1f, .9f, .4f));
                    lowerPreview = Preview("E 다리", start, lower, new Color(1f, .72f, .25f, .4f));
                    if (kind == Kind.Billboard)
                    {
                        Panel("광고판 회전축", start, new Vector2(.36f, .88f), new Color(.45f, 1f, .9f));
                        billboardFace = Panel("회전판", start, Vector2.one, new Color(.13f, .58f, .53f, .65f));
                        BillboardFace(new Vector2(upper.x, lower.y));
                    }
                    break;
            }
        }

        private GameObject Child(string label)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(transform, false);
            return obj;
        }

        private MagneticRail CreateRail(Vector2 first, Vector2 last, int piece, bool overhead = false, float reach = 1.1f)
        {
            MagneticRail rail = Child(overhead ? "천장 주행선" : "상층 레일").AddComponent<MagneticRail>();
            rail.Configure(first, last, reach, overhead, this, piece);
            rail.enabled = false;
            return rail;
        }

        private SpriteRenderer Panel(string label, Vector2 position, Vector2 size, Color color)
        {
            var renderer = Child(label).AddComponent<SpriteRenderer>();
            renderer.transform.position = position;
            renderer.sprite = ScorePickup.Icon;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = 2;
            return renderer;
        }

        private void BuildBridge()
        {
            Vector2 delta = lower - start;
            bridgeVisual = Panel("하층 다리", (start + lower) * .5f + Vector2.down * .12f,
                new Vector2(delta.magnitude, .24f), new Color(1f, .72f, .25f));
            Bridge = bridgeVisual.gameObject.AddComponent<BoxCollider2D>();
            Bridge.gameObject.layer = LayerMask.NameToLayer("Ground");
            Bridge.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            Bridge.size = bridgeVisual.size;
            Bridge.enabled = bridgeVisual.enabled = false;
        }

        private void BillboardFace(Vector2 end)
        {
            Vector2 span = end - start;
            billboardFace.transform.position = (start + end) * .5f + Vector2.down * .32f;
            billboardFace.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
            billboardFace.size = new Vector2(span.magnitude, .54f);
        }

        private void BuildConvoy()
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 roof = start + new Vector2(4f + i * 7.4f, .05f + i * .42f);
                var shell = Panel("축제 차량 " + (i + 1), roof - Vector2.up * .25f, new Vector2(4.8f, .5f),
                    i == 0 ? new Color(.18f, .75f, .8f) : new Color(.94f, .52f, .24f));
                var window = Panel("차량 창", roof + new Vector2(.65f, -.16f), new Vector2(2.45f, .18f), new Color(.08f, .18f, .26f));
                window.transform.SetParent(shell.transform, true);
                window.sortingOrder = 3;
                foreach (float wheelX in new[] { -1.55f, 1.55f })
                {
                    SpriteRenderer wheel = Panel("차량 바퀴", roof + new Vector2(wheelX, -.49f),
                        new Vector2(.36f, .23f), new Color(.08f, .12f, .2f));
                    wheel.transform.SetParent(shell.transform, true);
                }
                // A box's vertical sides can block a runner below the roof.
                // Only the roof edge participates in jumps and landings.
                var collider = shell.gameObject.AddComponent<EdgeCollider2D>();
                collider.points = new[] { new Vector2(-2.4f, .25f), new Vector2(2.4f, .25f) };
                collider.usedByEffector = true;
                shell.gameObject.AddComponent<PlatformEffector2D>();
                shell.gameObject.layer = LayerMask.NameToLayer("Ground");
                MovingPlatform platform = shell.gameObject.AddComponent<MovingPlatform>();
                var travel = new Vector2(1.5f + i * .65f, i == 0 ? .16f : -.18f);
                float pace = Mathf.Clamp(upper.y - start.y, 1f, 2.2f);
                platform.Configure(travel, (i == 0 ? 3.9f : 2.7f) / pace);
                platform.ConfigureRouteContact(this, i);
            }
        }

        private void BuildChain()
        {
            chain = new MagneticRail[3];
            chainPreviews = new LineRenderer[chain.Length];
            float length = (upper.x - start.x) / chain.Length;
            for (int i = 0; i < chain.Length; i++)
            {
                Vector2 first = start + new Vector2(i * length, i * .27f);
                Vector2 end = start + new Vector2((i + 1) * length - 1f, (i + 1) * .27f);
                chain[i] = CreateRail(first, end, i, reach: 1.35f);
                chainPreviews[i] = Preview("닫힌 전광판 " + (i + 1), first, end, new Color(1f, .48f, .72f, .5f), .38f);
                Panel("전광판 연결점", first + Vector2.down * .22f, new Vector2(.28f, .55f), new Color(.9f, .53f, .76f));
            }
        }

        private LineRenderer Preview(string label, Vector2 first, Vector2 last, Color color, float width = .07f)
        {
            LineRenderer line = Child(label).AddComponent<LineRenderer>();
            line.sharedMaterial = StageBootstrap.ParticleMaterial;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.sortingOrder = 1;
            line.SetPosition(0, first);
            line.SetPosition(1, last);
            return line;
        }

        public bool CanShape(PlayerMotor player)
        {
            if (!isActiveAndEnabled || !CanAct(player)) return false;
            Vector2 position = player.GetComponent<Rigidbody2D>().position;
            return position.x >= start.x - 10f && position.x < start.x - 1f && Mathf.Abs(position.y - start.y) < 6f;
        }

        public bool TryShape(PlayerMotor player, int mode, float heldStep = .02f)
        {
            if ((mode != 1 && mode != -1) || !CanShape(player)) return false;
            if (kind == Kind.Convoy || kind == Kind.Ceiling || (kind == Kind.Chain && mode > 0)) return false;
            bool adjusting = kind == Kind.Billboard && mode > 0 && Charge < 1f;
            if (Choice == mode && !adjusting) return false;

            bool first = Choice == 0;
            bool changed = Choice != mode;
            Choice = mode;
            if (kind == Kind.Chain)
            {
                openTime = 0f;
                OpenChain();
            }
            else
            {
                if (kind == Kind.Billboard)
                {
                    Charge = mode > 0 ? Mathf.Clamp01(Charge + heldStep / .7f) : 0f;
                    var end = new Vector2(upper.x, Mathf.Lerp(lower.y, upper.y, Charge));
                    Rail.Configure(start, end, 1.1f, false, this, 0);
                    upperPreview.SetPosition(1, end);
                    BillboardFace(end);
                }
                Rail.enabled = mode > 0;
                Bridge.enabled = bridgeVisual.enabled = mode < 0;
                upperPreview.enabled = mode < 0;
                lowerPreview.enabled = mode > 0;
            }
            if (first) GameSession.Active.RouteBuilt();
            if (changed) GameSession.Active.Feedback(start, mode > 0 ? Color.cyan : Color.yellow, GameAudio.Cue.MagnetAttract);
            return true;
        }

        public void RailTravel(PlayerMotor player, int piece, float travel)
        {
            if (Choice == 0) return;
            if (kind == Kind.Chain && travel >= .6f) TouchPiece(player, piece);
            else if ((kind == Kind.Switch || kind == Kind.Billboard) && travel >= 4f) Reward(player);
        }

        public void LandOnCeilingExit(PlayerMotor player, float dropX, float x)
        {
            if (kind == Kind.Ceiling && x >= dropX && x <= dropX + 10f) Reward(player);
        }

        internal void TouchPiece(PlayerMotor player, int piece)
        {
            if (rewarded || !CanAct(player)) return;
            int deaths = GameSession.Active.Deaths;
            if (contactDeaths != deaths)
            {
                contactDeaths = deaths;
                lastPiece = -1;
                distinctPieces = 0;
            }
            if (piece <= lastPiece) return;
            lastPiece = piece;
            if (++distinctPieces >= 2) Reward(player);
        }

        private void Reward(PlayerMotor player)
        {
            if (rewarded || !CanAct(player)) return;
            rewarded = true;
            GameSession.Active.CompleteFacility(kind, routeName);
        }

        private static bool CanAct(PlayerMotor player)
        {
            GameSession session = GameSession.Active;
            return player != null && session != null && session.Player == player && player.CanMove
                && !session.Paused && !session.Finished && !session.Respawning && !session.ChangingScene && Time.timeScale > 0f;
        }

        private void OpenChain()
        {
            for (int i = 0; i < chain.Length; i++)
            {
                if (openTime < i * .18f || chain[i].enabled) continue;
                chain[i].enabled = true;
                chainPreviews[i].enabled = false;
            }
        }

        private void Update()
        {
            queued = kind != Kind.Ceiling && kind != Kind.Convoy && CanShape(GameSession.Active?.Player)
                ? (Input.GetKey(KeyCode.E) ? -1 : Input.GetKey(KeyCode.Q) ? 1 : 0) : 0;
        }

        private void FixedUpdate()
        {
            PlayerMotor player = GameSession.Active?.Player;
            if (!CanAct(player)) return;
            if (queued != 0) TryShape(player, queued, Time.fixedDeltaTime);
            if (kind == Kind.Chain && Choice != 0 && openTime < .36f)
            {
                openTime += Time.fixedDeltaTime;
                OpenChain();
            }
        }
    }
}
