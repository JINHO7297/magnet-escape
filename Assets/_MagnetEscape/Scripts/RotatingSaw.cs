using UnityEngine;
namespace MagnetEscape
{
    public sealed class RotatingSaw : MonoBehaviour
    {
        private void Update() => transform.Rotate(0f, 0f, -180f * Time.deltaTime);
    }
}
