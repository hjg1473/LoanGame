using System.Collections.Generic;
using UnityEngine;

namespace LoadGame.Map
{
    public sealed class BuildingInstance
    {
        public string InstanceId { get; }
        public BuildingDefinition Definition { get; }
        public Vector2Int OriginCell { get; }
        public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;
        public BuildingQuality Quality { get; }
        public double ProductionMultiplier { get; }
        public double ValueMultiplier { get; }
        public float QualityProbability { get; }
        public string VisualSpritePath { get; }

        private readonly List<Vector2Int> occupiedCells;

        public BuildingInstance(
            string instanceId,
            BuildingDefinition definition,
            Vector2Int originCell,
            List<Vector2Int> cells,
            BuildingQuality quality,
            double productionMultiplier,
            double valueMultiplier,
            float qualityProbability,
            string visualSpritePath)
        {
            InstanceId = instanceId;
            Definition = definition;
            OriginCell = originCell;
            occupiedCells = cells;
            Quality = quality;
            ProductionMultiplier = productionMultiplier;
            ValueMultiplier = valueMultiplier;
            QualityProbability = Mathf.Clamp01(qualityProbability);
            VisualSpritePath = visualSpritePath;
        }

        public double GetProductionPerSecond()
        {
            return Definition == null ? 0d : Definition.productionPerSecond * ProductionMultiplier;
        }

        public double GetBuildingValue()
        {
            return Definition == null ? 0d : Definition.BuildingCityValue * ValueMultiplier;
        }
    }
}
