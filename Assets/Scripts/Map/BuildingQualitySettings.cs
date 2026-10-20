using System;
using UnityEngine;

namespace LoadGame.Map
{
    [Serializable]
    public sealed class BuildingQualitySetting
    {
        public BuildingQuality quality = BuildingQuality.Normal;
        [Min(0f)] public float weight = 85f;
        [Min(0f)] public double productionMultiplier = 1d;
        [Min(0f)] public double valueMultiplier = 1d;
    }

    public readonly struct BuildingQualityRoll
    {
        public BuildingQualityRoll(
            BuildingQuality quality,
            double productionMultiplier,
            double valueMultiplier,
            float probability)
        {
            Quality = quality;
            ProductionMultiplier = productionMultiplier;
            ValueMultiplier = valueMultiplier;
            Probability = probability;
        }

        public BuildingQuality Quality { get; }
        public double ProductionMultiplier { get; }
        public double ValueMultiplier { get; }
        public float Probability { get; }
    }

    [CreateAssetMenu(fileName = "BuildingQualitySettings", menuName = "Game/Building Quality Settings")]
    public sealed class BuildingQualitySettings : ScriptableObject
    {
        [SerializeField] private bool debugForceRainbow;
        [SerializeField] private BuildingQualitySetting[] qualities =
        {
            new BuildingQualitySetting
            {
                quality = BuildingQuality.Normal,
                weight = 85f,
                productionMultiplier = 1d,
                valueMultiplier = 1d
            },
            new BuildingQualitySetting
            {
                quality = BuildingQuality.Silver,
                weight = 10f,
                productionMultiplier = 2d,
                valueMultiplier = 2d
            },
            new BuildingQualitySetting
            {
                quality = BuildingQuality.Gold,
                weight = 4f,
                productionMultiplier = 4d,
                valueMultiplier = 4d
            },
            new BuildingQualitySetting
            {
                quality = BuildingQuality.Rainbow,
                weight = 1f,
                productionMultiplier = 8d,
                valueMultiplier = 8d
            }
        };

        public bool DebugForceRainbow => debugForceRainbow;

        public BuildingQualityRoll Roll()
        {
            if (debugForceRainbow)
            {
                return GetRoll(BuildingQuality.Rainbow, GetTotalWeight());
            }

            float totalWeight = GetTotalWeight();

            if (totalWeight <= 0f)
            {
                return CreateNormalRoll();
            }

            float value = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < qualities.Length; i++)
            {
                BuildingQualitySetting setting = qualities[i];
                if (setting == null || setting.weight <= 0f)
                {
                    continue;
                }

                value -= setting.weight;
                if (value <= 0f)
                {
                    return CreateRoll(setting, setting.weight / totalWeight);
                }
            }

            return CreateNormalRoll();
        }

        private BuildingQualityRoll GetRoll(BuildingQuality quality, float totalWeight)
        {
            if (qualities != null)
            {
                for (int i = 0; i < qualities.Length; i++)
                {
                    if (qualities[i] != null && qualities[i].quality == quality)
                    {
                        float probability = totalWeight > 0f ? qualities[i].weight / totalWeight : 0f;
                        return CreateRoll(qualities[i], probability);
                    }
                }
            }

            return quality == BuildingQuality.Rainbow
                ? new BuildingQualityRoll(BuildingQuality.Rainbow, 8d, 8d, 0f)
                : CreateNormalRoll();
        }

        private float GetTotalWeight()
        {
            float totalWeight = 0f;
            if (qualities == null)
            {
                return totalWeight;
            }

            for (int i = 0; i < qualities.Length; i++)
            {
                if (qualities[i] != null && qualities[i].weight > 0f)
                {
                    totalWeight += qualities[i].weight;
                }
            }

            return totalWeight;
        }

        private static BuildingQualityRoll CreateRoll(BuildingQualitySetting setting, float probability)
        {
            return new BuildingQualityRoll(
                setting.quality,
                Math.Max(0d, setting.productionMultiplier),
                Math.Max(0d, setting.valueMultiplier),
                Mathf.Clamp01(probability));
        }

        public static BuildingQualityRoll CreateNormalRoll()
        {
            return new BuildingQualityRoll(BuildingQuality.Normal, 1d, 1d, 1f);
        }
    }
}
