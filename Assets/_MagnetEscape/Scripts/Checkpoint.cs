using UnityEngine;
namespace MagnetEscape
{
    public sealed class Checkpoint : MonoBehaviour
    {
        public int Order;
        [SerializeField] private Transform respawnMarker;
        [SerializeField] private Vector2 spawnOffset = Vector2.up;
        private bool activated;
        private SpriteRenderer flag;
        public Vector2 SpawnPoint => respawnMarker != null ? (Vector2)respawnMarker.position
            : (Vector2)transform.position + spawnOffset;

        public void CreateRespawnMarker()
        {
            if (respawnMarker != null) return;
            GameObject marker = new GameObject("Respawn point");
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = spawnOffset;
            respawnMarker = marker.transform;
        }

        public void Activate()
        {
            GameArt.Apply(gameObject, "checkpoint_on", 3);
            flag = GetComponent<SpriteRenderer>();
            activated = true;
        }

        private void Update()
        {
            if (!activated || flag == null) return;
            float light = 0.88f + Mathf.Sin(Time.time * 3f) * 0.12f;
            flag.color = new Color(light, light, light, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.35f, 1f, 0.75f);
            Gizmos.DrawLine(transform.position, SpawnPoint);
            Gizmos.DrawWireCube(SpawnPoint, new Vector3(0.7f, 0.9f, 0f));
        }
    }
}
