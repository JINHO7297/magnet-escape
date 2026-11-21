using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    // All five scenes keep a serialized bootstrap reference. Visuals share authored sprites.
    public sealed class StageBootstrap : MonoBehaviour
    {
        [SerializeField, Range(0, StageInfo.EndlessStage)] private int stage;
        private static Sprite square;
        private static Material particleMaterial;
        private static PhysicsMaterial2D robotMaterial;
        private int checkpointOrder;
        public static Material ParticleMaterial
        {
            get
            {
                if (particleMaterial == null) particleMaterial = new Material(Shader.Find("Sprites/Default"));
                return particleMaterial;
            }
        }

        private void Awake() => BuildScene();

        public void BuildScene(System.Func<GameObject, GameObject> instantiatePrefab = null)
        {
            if (GetComponent<GameSession>() != null) return;
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(4f, 3f, -10f);
            cameraObject.AddComponent<AudioListener>();

            GameSession session = gameObject.AddComponent<GameSession>();
            session.Stage = stage;
            if (stage == 0)
            {
                session.FactoryWall = GameArt.Backdrop(24f);
                return;
            }

            StageDefinition layout = StageInfo.Definition(stage);
            string configurationError = layout == null || layout.Number != stage
                ? "Missing or mismatched stage definition: Resources/Stages/Stage" + stage
                : LayerMask.NameToLayer("Ground") < 0 ? "The Ground physics layer is missing." : null;
            if (configurationError == null && layout.PlayerPrefab != null)
                configurationError = layout.PlayerPrefabError();
            if (configurationError != null)
            {
                if (!Application.isPlaying) throw new System.InvalidOperationException(configurationError);
                Debug.LogError(configurationError);
                session.ReportCourseError("코스를 준비하지 못했습니다. 코스 선택으로 돌아가거나 다시 시도하세요.");
                return;
            }
            GameObject player;
            if (layout.PlayerPrefab != null)
            {
                player = instantiatePrefab != null ? instantiatePrefab(layout.PlayerPrefab)
                    : Instantiate(layout.PlayerPrefab, layout.StartPosition, Quaternion.identity);
                player.transform.position = layout.StartPosition;
                player.transform.rotation = Quaternion.identity;
                session.Player = player.GetComponent<PlayerMotor>();
            }
            else
            {
                player = Box("Magnet skater", layout.StartPosition, new Vector2(0.7f, 0.9f), Color.cyan, false);
                GameArt.Apply(player, "robot_idle", 10);
                Rigidbody2D body = player.AddComponent<Rigidbody2D>();
                if (robotMaterial == null) robotMaterial = new PhysicsMaterial2D("Robot frictionless") { friction = 0f, bounciness = 0f };
                body.sharedMaterial = robotMaterial;
                session.Player = player.AddComponent<PlayerMotor>();
                player.AddComponent<PlayerVisual>();
            }
            session.Player.DashEnabled = layout.DashEnabled;
            session.Player.MagnetEnabled = layout.MagnetEnabled;
            session.Player.RailEnabled = (layout.Rails != null && layout.Rails.Length > 0) || (layout.Routes != null && layout.Routes.Length > 0);
            session.Player.enabled = true;
            player.SetActive(true);

            if (stage == StageInfo.EndlessStage)
            {
                session.Player.RailEnabled = true;
                session.ConfigureCourse(1000000f, -7f, 0f, 999999f);
                session.FactoryWall = GameArt.Backdrop(160f);
                gameObject.AddComponent<EndlessCourse>().Initialize(System.Environment.TickCount);
                return;
            }
            foreach (StageDefinition.RectPlacement item in layout.Platforms)
                Platform(item.Position.x, item.Position.y, item.Width);
            foreach (var deck in layout.MidDecks)
                ElevatedDeck(deck, false);
            foreach (var ceiling in layout.InversionCeilings)
                ElevatedDeck(ceiling, true);
            session.Player.ConfigureCeilings(layout.InversionCeilings);
            if (layout.Rails != null)
                foreach (StageDefinition.RailPlacement item in layout.Rails)
                {
                    GameObject rail = new GameObject("자석 레일");
                    rail.AddComponent<MagneticRail>().Configure(item.Start, item.End, item.CatchRange);
                }
            if (layout.Routes != null)
                foreach (StageDefinition.RoutePlacement item in layout.Routes)
                {
                    GameObject route = new GameObject(item.Label);
                    route.AddComponent<RouteShaper>().Configure(item.Start, item.UpperEnd, item.LowerEnd, item.Label, item.Facility);
                }
            foreach (StageDefinition.MovingPlacement item in layout.MovingPlatforms)
                Moving(item.Position.x, item.Position.y, item.Travel, item.Period, item.Width);
            foreach (StageDefinition.RectPlacement item in layout.Spikes)
                Spikes(item.Position.x, item.Position.y, item.Width);
            foreach (StageDefinition.SawPlacement item in layout.Saws)
                Saw(item.Position.x, item.Position.y, item.Travel, item.Period, item.Phase);
            foreach (StageDefinition.MagnetPlacement item in layout.Magnets)
                Anchor(item.Position.x, item.Position.y, item.Range, item.Strength);
            foreach (StageDefinition.CheckpointPlacement item in layout.Checkpoints)
                CheckpointAt(item.Position.x, item.Position.y, item.SpawnOffset);
            foreach (StageDefinition.HintPlacement item in layout.Hints)
                Sign(item.Position.x, item.Position.y, item.Message, item.Range);

            GameObject gate = Box("Exit", layout.ExitPosition, new Vector2(0.6f, 2f), Color.green, true);
            GameArt.Apply(gate, "exit", 3);
            gate.AddComponent<ExitGate>().PrepareVisuals();
            session.ConfigureCourse(layout.ExitPosition.x, layout.FallDeathHeight, layout.RespawnDelay, layout.ParTime);
            session.FactoryWall = GameArt.Backdrop(layout.StartPosition, layout.ExitPosition);
        }

        private static GameObject Box(string name, Vector2 position, Vector2 size, Color color, bool trigger)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            if (square == null)
            {
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            }
            renderer.sprite = square;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = trigger;
            return obj;
        }

        internal static GameObject Platform(float x, float y, float width)
        {
            GameObject platform = Box("Rooftop platform", new Vector2(x, y), new Vector2(width, 0.6f), new Color(0.28f, 0.34f, 0.43f), false);
            platform.layer = LayerMask.NameToLayer("Ground");
            GameArt.Apply(platform, "platform", 0, true);
            return platform;
        }

        internal static GameObject ElevatedDeck(StageDefinition.RectPlacement item, bool ceiling)
        {
            var deck = Box(ceiling ? "반전 천장" : "중층 발판", item.Position, new Vector2(item.Width, .6f),
                ceiling ? new Color(.24f, .18f, .32f) : new Color(.16f, .28f, .32f), false);
            deck.layer = LayerMask.NameToLayer("Ground");
            var edge = new GameObject("주행면");
            edge.transform.SetParent(deck.transform, false);
            edge.transform.localPosition = new Vector2(0f, ceiling ? -.25f : .25f);
            var stripe = edge.AddComponent<SpriteRenderer>();
            stripe.sprite = square;
            stripe.drawMode = SpriteDrawMode.Sliced;
            stripe.size = new Vector2(item.Width, .1f);
            stripe.color = ceiling ? new Color(.84f, .66f, 1f) : new Color(.53f, .84f, .87f);
            stripe.sortingOrder = 1;
            var collider = deck.GetComponent<BoxCollider2D>();
            collider.usedByEffector = true;
            var effector = deck.AddComponent<PlatformEffector2D>();
            effector.rotationalOffset = ceiling ? 180f : 0f;
            effector.surfaceArc = 150f;
            return deck;
        }

        private static void Moving(float x, float y, Vector2 travel, float period, float width)
        {
            GameObject platform = Platform(x, y, width);
            platform.name = "Moving platform";
            platform.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.6f, 0.2f);
            platform.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            platform.AddComponent<MovingPlatform>().Configure(travel, period);
        }

        private void CheckpointAt(float x, float y, Vector2 spawnOffset)
        {
            GameObject flag = Box("Checkpoint", new Vector2(x, y), new Vector2(0.5f, 1.2f), Color.yellow, true);
            GameArt.Apply(flag, "checkpoint_off", 3);
            Checkpoint checkpoint = flag.AddComponent<Checkpoint>();
            checkpoint.Order = ++checkpointOrder;
            checkpoint.SpawnOffset = spawnOffset;
            checkpoint.CreateRespawnMarker();
        }

        private static void Sign(float x, float y, string message, float range)
        {
            GameObject obj = new GameObject("Tutorial: " + message);
            obj.transform.position = new Vector2(x, y);
            TutorialSign hint = obj.AddComponent<TutorialSign>();
            hint.Message = message;
            hint.Range = range;
        }

        internal static GameObject Spikes(float x, float y, float width)
        {
            GameObject spikes = Box("Spikes", new Vector2(x, y - 0.1f), new Vector2(width, 0.5f), Color.red, true);
            GameArt.Apply(spikes, "spike", 5, true);
            spikes.GetComponent<BoxCollider2D>().enabled = false;
            PolygonCollider2D polygon = spikes.AddComponent<PolygonCollider2D>();
            polygon.isTrigger = true;
            polygon.pathCount = 1;
            // Follow the visible teeth, including a clipped final tile, rather than
            // killing the player inside the transparent corners of a bounding box.
            float left = -width * 0.5f;
            float right = width * 0.5f;
            var points = new List<Vector2> { new Vector2(left, -0.24f), new Vector2(left, -0.15f) };
            for (int i = 0; i < Mathf.CeilToInt(width / 0.5f); i++)
            {
                float start = left + i * 0.5f;
                float peak = start + 0.25f;
                float end = Mathf.Min(start + 0.5f, right);
                if (peak < end) points.Add(new Vector2(peak, 0.2f));
                float height = end <= peak ? Mathf.Lerp(-0.15f, 0.2f, (end - start) / 0.25f)
                    : Mathf.Lerp(0.2f, -0.15f, (end - peak) / 0.25f);
                points.Add(new Vector2(end, height));
            }
            points.Add(new Vector2(right, -0.24f));
            polygon.SetPath(0, points.ToArray());
            spikes.AddComponent<Hazard>();
            return spikes;
        }

        private static void Saw(float x, float y, Vector2 travel, float period, float phase)
        {
            GameObject saw = Box("Patrol drone", new Vector2(x, y), Vector2.one, Color.white, true);
            // Keep the invisible box disabled immediately; use a matching circular trigger.
            saw.GetComponent<BoxCollider2D>().enabled = false;
            CircleCollider2D circle = saw.AddComponent<CircleCollider2D>();
            circle.radius = 0.45f;
            circle.isTrigger = true;
            GameArt.Apply(saw, "saw", 5);
            saw.AddComponent<Hazard>();
            saw.AddComponent<RotatingSaw>().Configure(travel, period, phase);
        }

        internal static GameObject Anchor(float x, float y, float range = 7f, float strength = 1f)
        {
            GameObject anchor = Box("Magnet anchor (range 7)", new Vector2(x, y), Vector2.one, Color.magenta, true);
            GameArt.Apply(anchor, "anchor", 4);
            anchor.AddComponent<MagnetAnchor>().Configure(range, strength);
            return anchor;
        }
    }
}
