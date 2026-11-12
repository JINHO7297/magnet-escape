using System.Collections.Generic;
using UnityEngine;

namespace MagnetEscape
{
    public sealed class MagnetAnchor : MonoBehaviour
    {
        public static readonly HashSet<MagnetAnchor> All = new HashSet<MagnetAnchor>();
        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);
    }
}
