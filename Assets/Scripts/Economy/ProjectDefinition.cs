using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoadGame.Economy
{
    public enum ProjectCategory
    {
        Industrial,
        Commercial,
        Financial,
        Residential,
        Special
    }

    [CreateAssetMenu(fileName = "ProjectDefinition", menuName = "Game/Project Definition")]
    public sealed class ProjectDefinition : ScriptableObject
    {
        public string projectId = "industrial_district";
        public string displayName = "Industrial District";
        [TextArea] public string description = "Develop an industrial district backed by factories, offices, and a stronger city value.";
        public Sprite icon;
        public int sortOrder;
        public double developmentCost = 2000d;
        public double requiredCityValue = 5000d;
        public double requiredPopulation;
        [Range(0f, 1f)] public float requiredEmploymentEfficiency;
        [Range(0f, 100f)] public float requiredHappiness;
        [Min(0f)] public float requiredPositiveNetSeconds;
        [Tooltip("All listed projects must be completed before this project unlocks.")]
        public List<string> prerequisiteProjectIds = new List<string>();
        public List<ProjectBuildingRequirement> requiredBuildings = new List<ProjectBuildingRequirement>
        {
            new ProjectBuildingRequirement { buildingType = BuildingType.Factory, requiredCount = 3 },
            new ProjectBuildingRequirement { buildingType = BuildingType.SmallOffice, requiredCount = 2 }
        };
        public double cityValueReward = 1000d;
        public ProjectCategory category = ProjectCategory.Industrial;
        public bool isFinalProject;
    }
}
