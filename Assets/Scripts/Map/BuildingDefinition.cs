using System;
using UnityEngine;

namespace LoadGame.Map
{
    public enum BuildingRole
    {
        None,
        Residential,
        Workplace,
        Leisure
    }

    public enum BuildingPlacementKind
    {
        Building,
        Road
    }

    public enum RoadType
    {
        Normal,
        Avenue
    }

    [CreateAssetMenu(fileName = "BuildingDefinition", menuName = "Game/Building Definition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        public string buildingId = "small_office";
        public string displayName = "Small Office";
        [TextArea] public string description;
        public Sprite icon;
        public GameObject prefab;
        public double cost = 500d;
        public double productionPerSecond = 10d;
        [Min(0f)] public double maintenancePerSecond;
        [Header("Placement")]
        public BuildingPlacementKind placementKind = BuildingPlacementKind.Building;
        public RoadType roadType = RoadType.Normal;
        [Header("City Role")]
        [Tooltip("None preserves the legacy building behavior and does not require workers.")]
        public BuildingRole buildingRole = BuildingRole.None;
        [Min(1)] public int starRating = 1;
        [Min(0)] public int populationCapacity;
        [Min(0)] public int requiredWorkers;
        [Min(0)] public int leisureCapacity;
        public int footprintWidth = 1;
        public int footprintHeight = 1;
        public Vector3 visualOffset;
        public double cityValue;
        [Header("Visual Variants")]
        [Tooltip("One path is selected when a building instance is placed. Leave empty to use Sprite Path.")]
        public string[] spritePaths = Array.Empty<string>();
        [Tooltip("Legacy/default Resources path used when Sprite Paths is empty.")]
        public string spritePath = "Sprites/Building2";

        [Header("Ground Visual")]
        [Tooltip("Replace every occupied ground cell with the configured tile sprite.")]
        public bool replaceGroundTile;
        [Tooltip("Resources path without file extension.")]
        public string groundSpritePath;

        public string id => buildingId;
        public long price => ToCashAmount(cost);
        public double cashPerSecond => productionPerSecond;
        public int width => footprintWidth;
        public int height => footprintHeight;
        public double BuildingCityValue => cityValue > 0d ? cityValue : cost;
        public bool IsRoad => placementKind == BuildingPlacementKind.Road;

        public string DefaultSpritePath
        {
            get
            {
                if (spritePaths != null)
                {
                    for (int i = 0; i < spritePaths.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(spritePaths[i]))
                        {
                            return spritePaths[i];
                        }
                    }
                }

                return spritePath;
            }
        }

        public string ChooseRandomSpritePath()
        {
            int validCount = 0;
            if (spritePaths != null)
            {
                for (int i = 0; i < spritePaths.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(spritePaths[i]))
                    {
                        validCount++;
                    }
                }
            }

            if (validCount == 0)
            {
                return spritePath;
            }

            int selected = UnityEngine.Random.Range(0, validCount);
            for (int i = 0; i < spritePaths.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(spritePaths[i]))
                {
                    continue;
                }

                if (selected == 0)
                {
                    return spritePaths[i];
                }

                selected--;
            }

            return spritePath;
        }

        private static long ToCashAmount(double amount)
        {
            return amount <= 0d ? 0L : (long)System.Math.Min(long.MaxValue, System.Math.Round(amount, System.MidpointRounding.AwayFromZero));
        }
    }
}
