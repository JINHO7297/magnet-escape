using UnityEngine;

namespace MagnetEscape
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ScorePickup : MonoBehaviour
    {
        private bool collected;
        private static Sprite icon;
        public static Sprite Icon
        {
            get
            {
                if (icon == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    icon = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), new Vector2(.5f,.5f), texture.width, 0, SpriteMeshType.FullRect);
                }
                return icon;
            }
        }
        public bool Collect(PlayerMotor player)
        {
            GameSession session = GameSession.Active;
            if (collected || session == null || session.Player != player || !session.IsEndless || session.Paused || session.Finished || session.ChangingScene) return false;
            collected = true; session.CollectStar();
            session.Feedback(transform.position, Color.yellow, GameAudio.Cue.Checkpoint);
            gameObject.SetActive(false); return true;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerMotor player = other.GetComponent<PlayerMotor>();
            if (player != null) Collect(player);
        }
    }
}
