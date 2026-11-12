using UnityEngine;
namespace MagnetEscape
{
    public sealed class Checkpoint : MonoBehaviour
    {
        public Vector2 SpawnPoint => (Vector2)transform.position + Vector2.up;
        public void Activate() => GetComponent<SpriteRenderer>().color = Color.green;
    }
}
