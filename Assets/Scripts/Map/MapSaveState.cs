using System;

namespace LoadGame.Map
{
    [Serializable]
    public sealed class MapCellSaveState
    {
        public int x;
        public int y;
    }

    [Serializable]
    public sealed class MapBuildingSaveState
    {
        public string buildingId;
        public int x;
        public int y;
        public int quality;
        public double productionMultiplier;
        public double valueMultiplier;
        public float qualityProbability;
    }
}
