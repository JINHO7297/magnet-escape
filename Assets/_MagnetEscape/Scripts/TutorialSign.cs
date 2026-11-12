using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    public sealed class TutorialSign : MonoBehaviour
    {
        private static readonly HashSet<TutorialSign> Signs = new HashSet<TutorialSign>();
        public string Message;
        public float Range = 4.5f;
        private void OnEnable() => Signs.Add(this);
        private void OnDisable() => Signs.Remove(this);

        public static string NearestMessage(Vector2 player)
        {
            string message = string.Empty;
            float best = float.MaxValue;
            foreach (TutorialSign sign in Signs)
            {
                float distance = Vector2.Distance(sign.transform.position, player);
                if (distance <= sign.Range && distance < best) { best = distance; message = sign.Message; }
            }
            return message;
        }
    }
}
