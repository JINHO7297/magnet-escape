using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    // Deterministic safe templates; stream a bounded window rather than one giant map.
    public sealed class EndlessCourse : MonoBehaviour
    {
        private readonly Queue<GameObject> chunks = new Queue<GameObject>();
        private System.Random random;
        private int nextChunk;
        private GameSession session;
        public int Seed { get; private set; }
        public int LiveChunks => chunks.Count;
        public int LastTemplate { get; private set; }
        public void Initialize(int seed)
        {
            Seed = seed; random = new System.Random(seed); session = GetComponent<GameSession>();
            Advance(0f);
        }
        private void Update()
        {
            if (session == null || session.Player == null || session.Paused || session.Finished || session.ChangingScene) return;
            Advance(session.Player.GetComponent<Rigidbody2D>().position.x);
            if (session.FactoryWall != null) session.FactoryWall.transform.position = new Vector3(session.Player.transform.position.x, session.FactoryWall.transform.position.y, session.FactoryWall.transform.position.z);
        }
        public void Advance(float x)
        {
            if (random == null) return;
            while (nextChunk * 32f < x + 112f) BuildChunk(nextChunk++);
            while (chunks.Count > 0 && chunks.Peek().transform.position.x + 32f < x - 64f) Destroy(chunks.Dequeue());
        }
        private void BuildChunk(int index)
        {
            float x = index * 32f;
            int template = index == 0 ? 0 : random.Next(4);
            LastTemplate = template;
            GameObject root = new GameObject("무한 구간 " + index + " / " + template); root.transform.position = new Vector2(x, 0);
            chunks.Enqueue(root);
            float gap = template == 2 && index > 2 ? Mathf.Min(5f, 2f + index / 12) : 0f;
            Attach(StageBootstrap.Platform(x + (32f - gap) / 2f, -.5f, 32f - gap), root);
            if (index > 0) Attach(StageBootstrap.Spikes(x + 15f, .15f, index > 20 ? 1.6f : 1f), root);
            if (template == 1 && index > 1) Attach(StageBootstrap.Spikes(x + 25f, .15f, 1f), root);
            if (template == 3 && index > 0)
            {
                GameObject obj = new GameObject("무한 광고판"); Attach(obj, root);
                obj.AddComponent<RouteShaper>().Configure(new Vector2(x + 8f, 1f), new Vector2(x + 28f, 2.7f), new Vector2(x + 28f, 1f), "보너스 경로 +250");
            }
            foreach (float offset in new[] { 12f, 15f, 18f })
            {
                GameObject star = new GameObject("별 +50 / 연속 보너스");
                star.transform.position = new Vector2(x + offset, 1.5f + (offset == 15f ? .5f : 0f));
                SpriteRenderer renderer = star.AddComponent<SpriteRenderer>();
                Texture2D texture = Texture2D.whiteTexture;
                renderer.sprite = ScorePickup.Icon;
                renderer.color = new Color(1f, .8f, .25f); renderer.sortingOrder = 5;
                star.transform.localScale = Vector3.one * .4f; star.transform.rotation = Quaternion.Euler(0,0,45);
                star.AddComponent<CircleCollider2D>().isTrigger = true; star.AddComponent<ScorePickup>(); Attach(star, root);
            }
        }
        private static void Attach(GameObject obj, GameObject root) => obj.transform.SetParent(root.transform, true);
    }
}
