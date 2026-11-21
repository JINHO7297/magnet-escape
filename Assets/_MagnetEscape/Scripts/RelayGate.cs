using UnityEngine;

namespace MagnetEscape
{
    [RequireComponent(typeof(CircleCollider2D), typeof(LineRenderer))]
    public sealed class RelayGate : MonoBehaviour
    {
        private RelayGate next;
        private bool armed;
        private LineRenderer ring;
        private static readonly Color ActiveColor = new Color(.45f, 1f, .74f);

        public void Configure(RelayGate following, bool first)
        {
            next = following;
            var trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .92f;
            ring = GetComponent<LineRenderer>();
            ring.sharedMaterial = StageBootstrap.ParticleMaterial;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            ring.startWidth = ring.endWidth = .1f;
            ring.sortingOrder = 7;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = 2f * Mathf.PI * i / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * trigger.radius);
            }
            SetArmed(first);
        }

        private void SetArmed(bool value)
        {
            armed = value;
            ring.startColor = ring.endColor = armed ? ActiveColor : new Color(.6f, .7f, .72f, .55f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var session = GameSession.Active;
            if (!armed || session == null || session.Paused || session.Finished || session.ChangingScene
                || session.Player == null || other.GetComponent<PlayerMotor>() != session.Player) return;
            if (next != null) next.SetArmed(true);
            else session.FinishRelay();
            session.Feedback(transform.position, ActiveColor, GameAudio.Cue.Checkpoint);
            gameObject.SetActive(false);
        }
    }
}
