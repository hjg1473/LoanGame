using System;
using LoadGame.Map;

namespace LoadGame.Economy
{
    [Serializable]
    public class ProjectBuildingRequirement
    {
        public BuildingDefinition building;
        public BuildingType buildingType = BuildingType.Factory;
        public int requiredCount = 1;
    }

    [Serializable]
    public sealed class ProjectRequiredBuilding : ProjectBuildingRequirement
    {
    }
}
