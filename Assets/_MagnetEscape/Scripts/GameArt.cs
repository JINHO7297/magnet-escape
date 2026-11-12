using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    // Committed sprites are loaded by name so generated courses share the same art.
    public static class GameArt
    {
        public const string BackdropName = "Factory wall - decoration only";
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => Sprites.Clear();

        public static Sprite Get(string name)
        {
            if (Sprites.TryGetValue(name, out Sprite sprite) && sprite != null) return sprite;
            sprite = Resources.Load<Sprite>("Art/" + name);
            if (sprite == null) Debug.LogError("Magnet Escape sprite missing: " + name);
            Sprites[name] = sprite;
            return sprite;
        }

        public static void Apply(GameObject obj, string name, int order = 0, bool tiled = false)
        {
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            Vector2 size = renderer.size;
            renderer.sprite = Get(name);
            renderer.color = Color.white;
            renderer.sortingOrder = order;
            renderer.drawMode = tiled ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple;
            if (tiled) renderer.size = size;
        }

        public static SpriteRenderer Backdrop(float length)
        {
            return Backdrop(new Vector2(-5f, 3f), new Vector2(length - 5f, 3f));
        }

        public static SpriteRenderer Backdrop(Vector2 start, Vector2 end)
        {
            GameObject backdrop = new GameObject(BackdropName);
            SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = Get("factory_backdrop");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.sortingOrder = -30;
            Bounds bounds = new Bounds(start, Vector3.zero);
            bounds.Encapsulate(end);
            FitBackdrop(renderer, bounds);
            return renderer;
        }

        public static void FitBackdrop(SpriteRenderer renderer, Bounds course)
        {
            renderer.transform.position = new Vector3(course.center.x, course.center.y, 0f);
            renderer.size = new Vector2(Mathf.Max(30f, course.size.x + 30f), Mathf.Max(24f, course.size.y + 24f));
        }
    }
}
