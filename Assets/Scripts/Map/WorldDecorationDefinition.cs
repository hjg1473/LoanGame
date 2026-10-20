using System;
using UnityEngine;

namespace LoadGame.Map
{
    public enum WorldDecorationType
    {
        Flower,
        Bush,
        Rock,
        Tree,
        Forest
    }

    [Serializable]
    public sealed class WorldDecorationDefinition
    {
        public WorldDecorationType type;
        public Sprite[] sprites = Array.Empty<Sprite>();
        public Vector3 worldOffset;
        public int sortingOrderOffset;
    }
}
