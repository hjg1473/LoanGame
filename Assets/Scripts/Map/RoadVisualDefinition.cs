using System;
using UnityEngine;

namespace LoadGame.Map
{
    [Serializable]
    public sealed class RoadVisualDefinition
    {
        [Range(0, 15)] public int connectionMask;
        public string spritePath = "Sprites/Load1";
        public bool flipY180;
    }
}
