using System;
using UnityEngine;

namespace MagnetEscape
{
    [CreateAssetMenu(menuName = "Magnet Escape/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [Range(1, 4)] public int Number = 1;
        public string DisplayName;
        [TextArea] public string Lesson;
        [Min(1f)] public float ParTime = 35f;
        public Vector2 StartPosition = Vector2.up;
        public GameObject PlayerPrefab;
        public Vector2 ExitPosition;
        public bool DashEnabled;
        public bool MagnetEnabled;
        public float FallDeathHeight = -7f;
        [Range(0f, 1f)] public float RespawnDelay = 0.2f;
        public RectPlacement[] Platforms = Array.Empty<RectPlacement>();
        public MovingPlacement[] MovingPlatforms = Array.Empty<MovingPlacement>();
        public RectPlacement[] Spikes = Array.Empty<RectPlacement>();
        public SawPlacement[] Saws = Array.Empty<SawPlacement>();
        public MagnetPlacement[] Magnets = Array.Empty<MagnetPlacement>();
        public CheckpointPlacement[] Checkpoints = Array.Empty<CheckpointPlacement>();
        public HintPlacement[] Hints = Array.Empty<HintPlacement>();

        public string PlayerPrefabError()
        {
            if (PlayerPrefab == null) return null;
            Rigidbody2D body = PlayerPrefab.GetComponent<Rigidbody2D>();
            BoxCollider2D collider = PlayerPrefab.GetComponent<BoxCollider2D>();
            if (PlayerPrefab.GetComponent<PlayerMotor>() == null || body == null || collider == null)
                return "Player prefab requires root PlayerMotor, BoxCollider2D and Rigidbody2D components.";
            if (body.bodyType != RigidbodyType2D.Dynamic || !body.simulated)
                return "Player prefab requires a dynamic Rigidbody2D with Simulated enabled.";
            if (!collider.enabled || collider.isTrigger)
                return "Player prefab requires an enabled, non-trigger root BoxCollider2D to stand on platforms.";
            return null;
        }

        [Serializable]
        public sealed class RectPlacement
        {
            public Vector2 Position;
            [Min(0.1f)] public float Width = 3f;
        }

        [Serializable]
        public sealed class MovingPlacement
        {
            public Vector2 Position;
            public Vector2 Travel;
            [Min(0.1f)] public float Period = 4f;
            [Min(0.1f)] public float Width = 3f;
        }

        [Serializable]
        public sealed class SawPlacement
        {
            public Vector2 Position;
            public Vector2 Travel;
            [Min(0.5f)] public float Period = 3.5f;
            public float Phase;
        }

        [Serializable]
        public sealed class MagnetPlacement
        {
            public Vector2 Position;
            [Min(0.5f)] public float Range = 7f;
            [UnityEngine.Range(0.1f, 3f)] public float Strength = 1f;
        }

        [Serializable]
        public sealed class CheckpointPlacement
        {
            public Vector2 Position;
            public Vector2 SpawnOffset = Vector2.up;
        }

        [Serializable]
        public sealed class HintPlacement
        {
            public Vector2 Position;
            [TextArea] public string Message;
            [Min(0.5f)] public float Range = 4.5f;
        }
    }
}
