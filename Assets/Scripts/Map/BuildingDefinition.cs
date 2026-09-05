using System;

namespace LoadGame.Map
{
    [Serializable]
    public sealed class BuildingDefinition
    {
        public string id = "small_office";
        public string displayName = "Small Office";
        public long price = 500L;
        public double cashPerSecond = 10d;
        public int width = 1;
        public int height = 1;
        public string spritePath = "Sprites/Building2";
    }
}
