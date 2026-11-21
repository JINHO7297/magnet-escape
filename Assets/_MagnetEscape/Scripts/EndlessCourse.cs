using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    public sealed class EndlessCourse : MonoBehaviour
    {
        private enum Template { Warmup, Double, Gap, Switch, Billboard, Ceiling, Convoy, Chain, Launch, Dive, Switchback, Viaduct }

        private const float ChunkLength = 32f;
        private readonly Queue<GameObject> chunks = new Queue<GameObject>();
        private readonly List<Template> bag = new List<Template>();
        private System.Random random;
        private int nextChunk;
        private float nextFloorY, nextStart;
        private int nextSpecial;
        private readonly List<Template> specialBag = new List<Template>();
        private Template lastSpecial;
        private int nextInversion = 2;
        private readonly List<StageDefinition.RectPlacement> ceilings = new List<StageDefinition.RectPlacement>();
        private bool hard;
        private float Between(float low, float high) => Mathf.Lerp(low, high, (float)random.NextDouble());
        private GameSession session;
        public int Seed { get; private set; }
        public int LiveChunks => chunks.Count;
        public int LastTemplate { get; private set; }

        public void Initialize(int seed)
        {
            Seed = seed;
            random = new System.Random(seed);
            session = GetComponent<GameSession>();
            hard = GameSession.HardEndless;
            Advance(0f);
        }

        private void Update()
        {
            if (session == null || session.Player == null || session.Paused || session.Finished || session.ChangingScene) return;
            Advance(session.Player.GetComponent<Rigidbody2D>().position.x);
        }

        public void Advance(float x)
        {
            if (random == null) return;
            while (nextChunk * ChunkLength < x + 112f) BuildChunk(nextChunk++);
            while (chunks.Count > 0 && chunks.Peek().transform.position.x + ChunkLength < x - 64f) Destroy(chunks.Dequeue());
        }

        private Template DrawTemplate(int index)
        {
            if (hard && index >= nextSpecial)
            {
                if (specialBag.Count == 0)
                    specialBag.AddRange(new[] { Template.Launch, Template.Dive, Template.Switchback, Template.Viaduct });
                int choice = random.Next(specialBag.Count);
                if (specialBag.Count > 1 && specialBag[choice] == lastSpecial)
                    choice = (choice + 1) % specialBag.Count;
                lastSpecial = specialBag[choice];
                specialBag.RemoveAt(choice);
                nextSpecial = index + random.Next(2, 4);
                return lastSpecial;
            }
            // Open with an empty stretch and two different techniques. The full
            // bag begins only after the first three chunks are behind the player.
            if (index == 0) return Template.Warmup;
            if (bag.Count == 0)
            {
                if (index == 1) bag.AddRange(new[] { Template.Billboard, Template.Ceiling });
                else
                    for (int i = (int)Template.Warmup; i <= (int)Template.Chain; i++)
                        bag.Add((Template)i);
            }
            int pick = random.Next(bag.Count);
            if ((int)bag[pick] == LastTemplate) pick = (pick + 1) % bag.Count;
            Template drawn = bag[pick];
            bag.RemoveAt(pick);
            return drawn;
        }

        private void BuildChunk(int index)
        {
            float x = index * ChunkLength;
            Template template = DrawTemplate(index);
            LastTemplate = (int)template;
            GameObject root = new GameObject("무한 구간 " + index + " / " + template);
            root.transform.position = new Vector2(x, 0);
            chunks.Enqueue(root);

            // Pick the next landing now so both sides of each gap agree.
            float level = Mathf.Clamp01(index / (hard ? 12f : 20f));
            float y = nextFloorY;
            float start = nextStart;
            nextStart = x + ChunkLength + Between(-1.5f, 1.5f);
            nextFloorY = Mathf.Clamp(y + Between(hard ? -1.35f : -.8f, hard ? 1.05f : .65f), -1.2f, 2.2f);
            float gap = hard ? Between(4.1f, 5.25f) + level * 1.2f : Between(2.3f, 3.65f) + level * 1.15f;
            if (index == 0) gap *= .7f;
            float edge = nextStart - gap;
            Attach(StageBootstrap.Platform((start + edge) * .5f, y - .5f, edge - start), root);
            Star(root, edge - .85f, y + .8f);
            Star(root, edge + gap * .5f, y + 2.2f);
            if ((int)template >= (int)Template.Launch)
            {
                BuildRelaySection(root, template, x, y);
                return;
            }
            if (index >= nextInversion)
            {
                var ceiling = Deck(x + 26f, y + Between(5.2f, 6.1f), 22f);
                Attach(StageBootstrap.ElevatedDeck(ceiling, true), root);
                ceilings.RemoveAll(old => old.Position.x + old.Width < x - 96f);
                ceilings.Add(ceiling);
                session.Player.ConfigureCeilings(ceilings.ToArray());
                Attach(StageBootstrap.Anchor(x + 15f, y + 5f), root);
                nextInversion = index + random.Next(3, 6);
            }
            if (index > 0)
            {
                bool crossesGap = template == Template.Gap || template == Template.Double;
                float center = crossesGap ? edge - 1f : x + Between(15f, 19f);
                var ledge = Deck(center, y + Between(1.35f, 2.15f), Between(5f, 8f));
                var platform = StageBootstrap.ElevatedDeck(ledge, false);
                Attach(platform, root);
                platform.AddComponent<MovingPlatform>().Configure(new Vector2(Between(1f, 3f), Between(.45f, hard ? 1.7f : 1f)), Between(2.8f, 4.5f));
            }
            float coneWidth = Mathf.Lerp(hard ? 1.15f : .9f, hard ? 1.65f : 1.25f, level);
            switch (template)
            {
                case Template.Warmup:
                    Stars(root, x, y, 9f, .55f, 14f, .75f, 19f, .55f);
                    break;
                case Template.Double:
                    Attach(StageBootstrap.Spikes(x + 11f, y + .15f, coneWidth), root);
                    Attach(StageBootstrap.Spikes(x + 18f, y + .15f, coneWidth), root);
                    Stars(root, x, y, 11f, 2.05f, 17f, .65f, 18f, 2.05f);
                    break;
                case Template.Gap:
                    Stars(root, x, y, 14f, .55f, 22f, .55f, 28f, 1.65f);
                    break;
                case Template.Switch:
                    Facility(root, x, y, 25f, 2.5f, RouteShaper.Kind.Switch, "임시 연결로");
                    Attach(StageBootstrap.Spikes(x + 17f, y + .15f, coneWidth), root);
                    Stars(root, x, y, 12f, 1.65f, 17f, 2.5f, 22f, 3.05f);
                    break;
                case Template.Billboard:
                    Facility(root, x, y, 21f, 3.6f, RouteShaper.Kind.Billboard, "회전 광고판");
                    Attach(StageBootstrap.Spikes(x + 14.5f, y + .15f, coneWidth), root);
                    Stars(root, x, y, 12f, 2f, 17f, 3f, 22f, 4.1f);
                    break;
                case Template.Ceiling:
                    Facility(root, x, y, 26f, 3.25f, RouteShaper.Kind.Ceiling, "천장 케이블");
                    Attach(StageBootstrap.Spikes(x + 17.5f, y + .15f, coneWidth), root);
                    Stars(root, x, y, 12f, 2.62f, 18f, 2.68f, 24f, 2.75f);
                    break;
                case Template.Convoy:
                    Facility(root, x, y, 24f, index > 20 ? 2.6f : 1.9f, RouteShaper.Kind.Convoy, "축제 차량");
                    Stars(root, x, y, 12f, 1.7f, 16f, 2.6f, 21f, 2.1f);
                    break;
                case Template.Chain:
                    Facility(root, x, y, 29f, 2.1f, RouteShaper.Kind.Chain, "연쇄 전광판");
                    Attach(StageBootstrap.Spikes(x + 17f, y + .15f, coneWidth), root);
                    Stars(root, x, y, 11f, 1.7f, 18f, 2.05f, 25f, 2.3f);
                    break;
            }
        }

        private void BuildRelaySection(GameObject root, Template template, float x, float y)
        {
            // The first ring marks the transfer point. The second shows the target lane.
            x += Between(-.7f, .7f);
            float rise = Between(-.35f, .55f);
            float landingShift = Between(-.7f, .9f);
            Vector2 first, second;
            string hint;
            switch (template)
            {
                case Template.Launch:
                    Rail(root, x, y, 5.5f, .7f, 18f, 3.6f + rise);
                    Ledge(root, x + 28f + landingShift, y + 4.25f + rise, 8f);
                    first = new Vector2(15.5f, 3.5f + rise * .8f);
                    second = new Vector2(23f + landingShift, 5.45f + rise);
                    hint = "점프 + Q로 오르막을 타고 초록 링에서 Shift로 발사!";
                    break;
                case Template.Dive:
                    Rail(root, x, y, 5f, 3.2f, 18.5f, 4.9f + rise, true);
                    Ledge(root, x + 25f + landingShift, y + 1.1f + rise, 6.5f);
                    first = new Vector2(16f, 4.1f + rise * .8f);
                    second = new Vector2(23.5f + landingShift, 2.05f + rise);
                    hint = "점프 + Q로 천장을 잡고 초록 링에서 Shift로 내려꽂으세요.";
                    break;
                case Template.Switchback:
                    Rail(root, x, y, 5f, .8f, 17f, 3.4f);
                    Rail(root, x, y, 14f, 4.5f, 26f + landingShift, 5.35f + rise, true);
                    Ledge(root, x + 28f, y + 1.55f, 6f);
                    first = new Vector2(14f, 3.15f);
                    second = new Vector2(25f + landingShift, 4.7f + rise);
                    hint = "오르막 끝에서 Q를 유지하고 Space로 위 케이블에 환승하세요.";
                    break;
                default:
                    Facility(root, x - 3f, y, 23f, 2.3f, RouteShaper.Kind.Convoy, "고가도로 차량");
                    Rail(root, x, y, 12f, 3.5f, 23f, 4.45f, true);
                    Ledge(root, x + 27f + landingShift, y + 1.4f + rise, 8f);
                    first = new Vector2(19f, 3.7f);
                    second = new Vector2(27f + landingShift, 2.3f + rise);
                    hint = "차량 지붕에서 위 케이블을 잡고 E로 내려와 두 번째 링을 통과하세요.";
                    break;
            }
            var sign = new GameObject("환승 안내");
            sign.transform.position = new Vector2(x + 6f, y + 1.5f);
            Attach(sign, root);
            var tutorial = sign.AddComponent<TutorialSign>();
            tutorial.Message = hint;
            tutorial.Range = 6f;
            var origin = new Vector2(x, y);
            var target = Gate(root, origin + second);
            target.Configure(null, false);
            Gate(root, origin + first).Configure(target, true);
        }

        private static RelayGate Gate(GameObject root, Vector2 position)
        {
            var obj = new GameObject("환승 링");
            obj.transform.position = position;
            Attach(obj, root);
            return obj.AddComponent<RelayGate>();
        }

        private static void Ledge(GameObject root, float x, float y, float width)
        {
            Attach(StageBootstrap.ElevatedDeck(Deck(x, y, width), false), root);
        }

        private static void Rail(GameObject root, float x, float y, float start, float height, float end, float endHeight, bool overhead = false)
        {
            var rail = new GameObject(overhead ? "환승 케이블" : "발사 레일");
            Attach(rail, root);
            rail.AddComponent<MagneticRail>().Configure(new Vector2(x + start, y + height),
                new Vector2(x + end, y + endHeight), 1.15f, overhead);
        }

        private static StageDefinition.RectPlacement Deck(float x, float y, float width) =>
            new StageDefinition.RectPlacement { Position = new Vector2(x, y), Width = width };

        private static void Facility(GameObject root, float x, float y, float end, float height, RouteShaper.Kind kind, string label)
        {
            var obj = new GameObject(label);
            Attach(obj, root);
            float startHeight = kind == RouteShaper.Kind.Chain ? 1.35f : 1.05f;
            obj.AddComponent<RouteShaper>().Configure(new Vector2(x + 7f, y + startHeight),
                new Vector2(x + end, y + height), new Vector2(x + 28f, y + 1.05f), label, kind);
        }

        private static void Stars(GameObject root, float x, float y, float first, float firstY, float second, float secondY, float third, float thirdY)
        {
            Star(root, x + first, y + firstY);
            Star(root, x + second, y + secondY);
            Star(root, x + third, y + thirdY);
        }

        private static void Star(GameObject root, float x, float y)
        {
            var star = new GameObject("별 +50 / 연속 보너스");
            star.transform.SetPositionAndRotation(new Vector2(x, y), Quaternion.Euler(0f, 0f, 45f));
            star.transform.localScale = Vector3.one * .4f;
            var renderer = star.AddComponent<SpriteRenderer>();
            renderer.sprite = ScorePickup.Icon;
            renderer.color = new Color(1f, .8f, .25f);
            renderer.sortingOrder = 5;
            star.AddComponent<CircleCollider2D>().isTrigger = true;
            star.AddComponent<ScorePickup>();
            Attach(star, root);
        }

        private static void Attach(GameObject obj, GameObject root) => obj.transform.SetParent(root.transform, true);
    }
}
