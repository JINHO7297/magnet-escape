using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    // All five scenes keep a serialized bootstrap reference. Visuals share authored sprites.
    public sealed class StageBootstrap : MonoBehaviour
    {
        [SerializeField, Range(0, 4)] private int stage;
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

        public void BuildScene()
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
                GameArt.Backdrop(24f);
                return;
            }

            GameObject player = Box("Magnet robot", new Vector2(0f, 1f), new Vector2(0.7f, 0.9f), Color.cyan, false);
            GameArt.Apply(player, "robot_idle", 10);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            if (robotMaterial == null) robotMaterial = new PhysicsMaterial2D("Robot frictionless") { friction = 0f, bounciness = 0f };
            body.sharedMaterial = robotMaterial;
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
                Platform(29f, 0.8f, 4f);
                CheckpointAt(29f, 1.7f);
                Platform(36f, 0f, 6f);
                Spikes(35f, 0.65f, 1.3f);
                Sign(1f, 1f, "A / D or arrows: move. Reach the green exit at the end of the course.");
                Sign(8f, 1f, "Tap SPACE for a low jump. Hold SPACE to jump higher.");
                Sign(15f, 2f, "Yellow flags save your checkpoint. Falling sends you back here.");
                Sign(23f, 1f, "Keep moving through your jump. Release SPACE early for a precise landing.");
                Sign(34f, 1f, "Jump over the spikes. R restarts the whole stage; Esc opens pause.");
                exit = 38f;
            }
            else if (stage == 2)
            {
                Moving(8f, -0.5f, new Vector2(3f, 1f));
                Platform(15f, 0f, 6f);
                CheckpointAt(14f, 0.9f);
                Saw(17f, 1.7f, new Vector2(0f, 0.8f), 3.6f);
                Platform(23f, 0f, 6f);
                Spikes(22f, 0.65f, 1.2f);
                Moving(31f, 0f, new Vector2(2f, 2f));
                Platform(37f, 0.8f, 5f);
                CheckpointAt(36f, 1.7f);
                Saw(38f, 2.4f, new Vector2(0f, 0.9f), 4.2f);
                Platform(44f, 0f, 6f);
                Spikes(43f, 0.65f, 1.5f);
                Moving(51f, 0f, new Vector2(2f, 0f));
                Platform(58f, 0f, 6f);
                Sign(1f, 1f, "Stand on moving platforms and wait for the right moment to jump.");
                Sign(14f, 1f, "Hold a direction and press SHIFT to dash. A diagonal dash works too.");
                Sign(25f, 1f, "The cyan bar shows dash cooldown. Wait for it to refill before dashing again.");
                Sign(36f, 2f, "The red rail shows the saw's patrol. Wait for an opening, then jump or dash.");
                Sign(47f, 1f, "Ride the last platform, then dash to safety. Watch its direction.");
                exit = 60f;
            }
            else if (stage == 3)
            {
                Anchor(8f, 3.5f, 7f, 1.05f);
                Platform(14f, -0.5f, 6f);
                CheckpointAt(14f, 0.4f);
                Anchor(20f, 4f);
                Platform(26f, 0f, 6f);
                Anchor(27f, 0f, 4.5f, 1.2f);
                Platform(38f, 0.5f, 6f);
                CheckpointAt(37f, 1.4f);
                Anchor(44f, 5.5f, 7f, 1.15f);
                Platform(50f, 1f, 6f);
                Sign(2f, 1f, "Jump toward the magnet, then hold Q to attract yourself toward it.");
                Sign(14f, 1f, "Magnet range is marked by a circle. Your tether shows the selected magnet.");
                Sign(25f, 1f, "Stand right of the low magnet, jump and hold E to launch right. Release to coast.");
                Sign(38f, 2f, "Release Q or E to leave the field. Combine a high jump, attraction and dash.");
                Sign(49f, 2f, "Opposite controls: Q pulls you closer; E pushes you away. Reach the exit.");
                exit = 52f;
            }
            else
            {
                Moving(8f, -0.5f, new Vector2(2f, 1.5f));
                Platform(14f, 0f, 5f);
                CheckpointAt(13f, 0.9f);
                Spikes(15f, 0.65f, 0.8f);
                Anchor(20f, 4f);
                Saw(20f, 1f, new Vector2(1.2f, 0.5f), 4f);
                Platform(26f, 0f, 6f);
                CheckpointAt(25f, 0.9f);
                Spikes(27f, 0.65f, 1f);
                Moving(32f, 0f, new Vector2(1f, 2f));
                Anchor(34f, 5f);
                Platform(39f, 0f, 6f);
                Saw(38f, 1.8f, new Vector2(0f, 0.9f), 3.8f);
                Platform(47f, 1f, 4f);
                CheckpointAt(46f, 1.9f);
                Moving(53f, 0.5f, new Vector2(3f, 1f));
                Anchor(55f, 5f, 7f, 1.1f);
                Platform(62f, 0f, 6f);
                Saw(61f, 1.5f, new Vector2(0f, 0.9f), 4.5f);
                CheckpointAt(63f, 0.9f);
                Platform(69f, 1f, 3f);
                Spikes(69f, 1.65f, 0.8f);
                Anchor(73f, 5f);
                Platform(79f, 0f, 6f);
                Saw(78f, 2.1f, new Vector2(1f, 0f), 3.6f);
                Sign(1f, 1f, "Final route: combine moving platforms, jumps, dash and magnetic momentum.");
                Sign(13f, 1f, "Save the checkpoint before crossing the magnetic gap.");
                Sign(26f, 1f, "Your dash is reusable after cooldown. Plan one safe landing at a time.");
                Sign(46f, 2f, "The last machinery section is ahead. Use the high magnet to stay above the saw.");
                Sign(64f, 1f, "Final stretch. Jump over the spikes, then use the last magnet to escape.");
                exit = 81f;
            }
            GameObject gate = Box("Exit", new Vector2(exit, stage == 3 ? 2.2f : 1.2f), new Vector2(0.6f, 2f), Color.green, true);
            GameArt.Apply(gate, "exit", 3);
            gate.AddComponent<ExitGate>();
            session.CourseEnd = exit;
            GameArt.Backdrop(exit);
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
            GameArt.Apply(platform, "platform", 0, true);
            return platform;
        }

        private static void Moving(float x, float y, Vector2 travel)
        {
            GameObject platform = Platform(x, y, 3f);
            platform.name = "Moving platform";
            platform.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.6f, 0.2f);
            platform.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            platform.AddComponent<MovingPlatform>().Travel = travel;
        }

        private void CheckpointAt(float x, float y)
        {
            GameObject flag = Box("Checkpoint", new Vector2(x, y), new Vector2(0.5f, 1.2f), Color.yellow, true);
            GameArt.Apply(flag, "checkpoint_off", 3);
            flag.AddComponent<Checkpoint>().Order = ++checkpointOrder;
        }

        private static void Sign(float x, float y, string message)
        {
            GameObject obj = new GameObject("Tutorial: " + message);
            obj.transform.position = new Vector2(x, y);
            obj.AddComponent<TutorialSign>().Message = message;
        }

        private static void Spikes(float x, float y, float width)
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
        }

        private static void Saw(float x, float y, Vector2 travel, float period)
        {
            GameObject saw = Box("Rotating saw", new Vector2(x, y), Vector2.one, Color.white, true);
            // Keep the invisible box disabled immediately; use a matching circular trigger.
            saw.GetComponent<BoxCollider2D>().enabled = false;
            CircleCollider2D circle = saw.AddComponent<CircleCollider2D>();
            circle.radius = 0.45f;
            circle.isTrigger = true;
            GameArt.Apply(saw, "saw", 5);
            saw.AddComponent<Hazard>();
            saw.AddComponent<RotatingSaw>().Configure(travel, period, 0f);
        }

        private static void Anchor(float x, float y, float range = 7f, float strength = 1f)
        {
            GameObject anchor = Box("Magnet anchor (range 7)", new Vector2(x, y), Vector2.one, Color.magenta, true);
            GameArt.Apply(anchor, "anchor", 4);
            anchor.AddComponent<MagnetAnchor>().Configure(range, strength);
        }
    }
}
