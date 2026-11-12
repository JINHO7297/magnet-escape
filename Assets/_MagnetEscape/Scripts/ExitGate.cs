using UnityEngine;
namespace MagnetEscape
{
    public sealed class ExitGate : MonoBehaviour
    {
        private LineRenderer outline;
        private SpriteRenderer door;
        private void Start() => PrepareVisuals();

        public void PrepareVisuals()
        {
            door = GetComponent<SpriteRenderer>();
            outline = GetComponent<LineRenderer>();
            if (outline == null) outline = gameObject.AddComponent<LineRenderer>();
            outline.sharedMaterial = StageBootstrap.ParticleMaterial;
            outline.useWorldSpace = false;
            outline.loop = true;
            outline.positionCount = 4;
            outline.sortingOrder = 4;
            outline.startWidth = outline.endWidth = 0.035f;
            outline.startColor = outline.endColor = new Color(0.35f, 0.95f, 0.75f, 0.5f);
            outline.SetPosition(0, new Vector3(-0.47f, -0.98f, 0f));
            outline.SetPosition(1, new Vector3(-0.47f, 0.98f, 0f));
            outline.SetPosition(2, new Vector3(0.47f, 0.98f, 0f));
            outline.SetPosition(3, new Vector3(0.47f, -0.98f, 0f));
        }

        private void Update()
        {
            if (outline == null) PrepareVisuals();
            GameSession session = GameSession.Active;
            bool nearby = session != null && session.Player != null
                && Vector2.Distance(session.Player.transform.position, transform.position) < 4f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            float alpha = nearby ? Mathf.Lerp(0.65f, 1f, pulse) : Mathf.Lerp(0.3f, 0.55f, pulse);
            outline.startColor = outline.endColor = new Color(0.35f, 1f, 0.75f, alpha);
            outline.startWidth = outline.endWidth = nearby ? 0.06f : 0.035f;
            if (door != null) door.color = session != null && session.Finished
                ? new Color(0.7f, 1f, 0.85f) : Color.white;
        }
    }
}
