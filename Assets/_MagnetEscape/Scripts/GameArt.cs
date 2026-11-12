using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    // Committed sprites are loaded by name so generated courses share the same art.
    public static class GameArt
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

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

        public static void Backdrop(float length)
        {
            GameObject backdrop = new GameObject("Factory wall - decoration only");
            backdrop.transform.position = new Vector3(length * 0.5f - 5f, 3f, 0f);
            SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = Get("factory_backdrop");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(length + 30f, 24f);
            renderer.sortingOrder = -30;
        }
    }
}
