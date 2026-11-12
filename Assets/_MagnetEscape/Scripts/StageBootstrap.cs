using UnityEngine;

namespace MagnetEscape
{
    // A small reproducible graybox. All five scenes keep a serialized bootstrap reference.
    // Replace primitives with authored prefabs after movement has been play-tested.
    public sealed class StageBootstrap : MonoBehaviour
    {
        [SerializeField, Range(0, 4)] private int stage;
        private static Sprite square;
        private static Material particleMaterial;
        public static Material ParticleMaterial
        {
            get
            {
                if (particleMaterial == null) particleMaterial = new Material(Shader.Find("Sprites/Default"));
                return particleMaterial;
            }
        }

        private void Awake()
        {
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
            if (stage == 0) return;

            GameObject player = Box("Magnet robot", new Vector2(0f, 1f), new Vector2(0.7f, 0.9f), Color.cyan, false);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.sharedMaterial = new PhysicsMaterial2D("Robot frictionless") { friction = 0f, bounciness = 0f };
            session.Player = player.AddComponent<PlayerMotor>();
            session.Player.DashEnabled = stage >= 2;
            session.Player.MagnetEnabled = stage >= 3;
            player.AddComponent<PlayerVisual>();

            Platform(1f, -0.5f, 8f);
            float exit;
            if (stage == 1)
            {
                Platform(9f, 0f, 4f);
                Platform(15f, 0.7f, 4f);
                Platform(22f, 0f, 6f);
                CheckpointAt(15f, 1.6f);
                Spikes(22f, 0.65f, 0.7f);
                exit = 24f;
            }
            else if (stage == 2)
            {
                Moving(8f, -0.5f, new Vector2(3f, 1f));
                Platform(15f, 0f, 6f);
                CheckpointAt(14f, 0.9f);
                Saw(17f, 1.7f);
                Platform(23f, 0f, 6f);
                Spikes(22f, 0.65f, 1.2f);
                exit = 25f;
            }
            else if (stage == 3)
            {
                Anchor(8f, 3.5f);
                Platform(14f, -0.5f, 6f);
                CheckpointAt(14f, 0.4f);
                Anchor(20f, 4f);
                Platform(26f, 0f, 6f);
                exit = 28f;
            }
            else
            {
                Moving(8f, -0.5f, new Vector2(2f, 1.5f));
                Platform(14f, 0f, 5f);
                CheckpointAt(13f, 0.9f);
                Spikes(15f, 0.65f, 0.8f);
                Anchor(20f, 4f);
                Saw(20f, 1f);
                Platform(26f, 0f, 6f);
                CheckpointAt(25f, 0.9f);
                Spikes(27f, 0.65f, 1f);
                Moving(32f, 0f, new Vector2(1f, 2f));
                Anchor(34f, 5f);
                Platform(39f, 0f, 6f);
                Saw(38f, 1.8f);
                exit = 41f;
            }
            Box("Exit", new Vector2(exit, 1.2f), new Vector2(0.6f, 2f), Color.green, true).AddComponent<ExitGate>();
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

        private static GameObject Platform(float x, float y, float width)
        {
            GameObject platform = Box("Factory platform", new Vector2(x, y), new Vector2(width, 0.6f), new Color(0.28f, 0.34f, 0.43f), false);
            platform.layer = LayerMask.NameToLayer("Ground");
            return platform;
        }

        private static void Moving(float x, float y, Vector2 travel)
        {
            GameObject platform = Platform(x, y, 3f);
            platform.name = "Moving platform";
            platform.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.6f, 0.2f);
            platform.AddComponent<Rigidbody2D>();
            platform.AddComponent<MovingPlatform>().Travel = travel;
        }

        private static void CheckpointAt(float x, float y)
        {
            Box("Checkpoint", new Vector2(x, y), new Vector2(0.5f, 1.2f), Color.yellow, true).AddComponent<Checkpoint>();
        }

        private static void Spikes(float x, float y, float width)
        {
            Box("Spikes (placeholder)", new Vector2(x, y), new Vector2(width, 0.7f), Color.red, true).AddComponent<Hazard>();
        }

        private static void Saw(float x, float y)
        {
            GameObject saw = Box("Rotating saw (placeholder)", new Vector2(x, y), Vector2.one * 0.85f, new Color(1f, 0.4f, 0.25f), true);
            saw.AddComponent<Hazard>();
            saw.AddComponent<RotatingSaw>();
        }

        private static void Anchor(float x, float y)
        {
            GameObject anchor = Box("Magnet anchor (range 7)", new Vector2(x, y), Vector2.one, Color.magenta, true);
            anchor.AddComponent<MagnetAnchor>();
            LineRenderer ring = anchor.AddComponent<LineRenderer>();
            ring.sharedMaterial = ParticleMaterial;
            ring.startColor = ring.endColor = new Color(0.5f, 0.2f, 0.5f, 0.35f);
            ring.startWidth = ring.endWidth = 0.025f;
            ring.loop = true;
            ring.useWorldSpace = false;
            ring.positionCount = 64;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 7f, Mathf.Sin(angle) * 7f, 0.1f));
            }
        }
    }
}
