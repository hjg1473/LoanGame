using System;
using UnityEngine;

namespace LoadGame.Map
{
    [Serializable]
    public sealed class WorldGenerationSettings
    {
        [Header("Seed")]
        public int worldSeed = 12345;

        [Header("Starting Development")]
        [Min(1)] public int startAreaWidth = 16;
        [Min(1)] public int startAreaHeight = 16;
        [Min(0)] public int roadRevealRadius = 3;
        [Min(0)] public int decorationClearRadius = 0;

        [Header("Exploration")]
        [Min(0)] public int startingRevealRadius = 2;
        [Min(0)] public int purchaseRevealRadius = 1;
        public bool revealIncludesDiagonals = true;
        public bool requireAdjacentOwnedLand = true;
        public bool debugRevealAll;

        [Header("Nature")]
        [Range(0f, 1f)] public float forestDensity = 0.48f;
        [Min(0.005f)] public float forestClusterScale = 0.075f;
        [Range(0f, 1f)] public float treeChance = 0.68f;
        [Range(0f, 1f)] public float bushChance = 0.12f;
        [Range(0f, 1f)] public float flowerChance = 0.11f;
        [Range(0f, 1f)] public float rockChance = 0.06f;

        [Header("Grass Biome Color")]
        public bool enableGrassNoiseGradient = true;
        public Color grassForestTint = new Color(0.82f, 0.93f, 0.84f, 1f);
        public Color grassBaseTint = Color.white;
        public Color grassMeadowTint = new Color(1f, 0.96f, 0.8f, 1f);
        [Min(0.001f)] public float grassLowFrequencyScale = 0.025f;
        [Min(0.001f)] public float grassHighFrequencyScale = 0.16f;
        [Range(0f, 0.25f)] public float grassHighFrequencyStrength = 0.06f;
        [Range(0f, 1f)] public float grassTintStrength = 0.35f;

        [Header("Water")]
        public bool generateWater = true;
        [Min(0.005f)] public float waterNoiseScale = 0.045f;
        [Range(0f, 1f)] public float waterThreshold = 0.77f;
        [Min(0)] public int waterMinimumStartDistance = 12;
        [Min(0)] public int waterGrassBufferRadius = 1;

        [Header("Reveal")]
        public bool animateRoadReveal = true;
        [Min(0f)] public float roadRevealStepDelay = 0.025f;

        public bool IsInsideStartArea(int x, int y)
        {
            int minX = -Mathf.FloorToInt(startAreaWidth * 0.5f);
            int minY = -Mathf.FloorToInt(startAreaHeight * 0.5f);
            return x >= minX && x < minX + startAreaWidth
                && y >= minY && y < minY + startAreaHeight;
        }

        public bool IsInitiallyRevealed(int x, int y)
        {
            if (RevealAllForDebug || IsInsideStartArea(x, y))
            {
                return true;
            }

            int minX = -Mathf.FloorToInt(startAreaWidth * 0.5f);
            int minY = -Mathf.FloorToInt(startAreaHeight * 0.5f);
            int maxX = minX + startAreaWidth - 1;
            int maxY = minY + startAreaHeight - 1;
            int distanceX = x < minX ? minX - x : x > maxX ? x - maxX : 0;
            int distanceY = y < minY ? minY - y : y > maxY ? y - maxY : 0;
            return Mathf.Max(distanceX, distanceY) <= startingRevealRadius;
        }

        public bool RevealAllForDebug
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return debugRevealAll;
#else
                return false;
#endif
            }
        }

        public bool ShouldGenerateWater(int x, int y)
        {
            if (!generateWater || IsInsideStartArea(x, y))
            {
                return false;
            }

            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) < waterMinimumStartDistance)
            {
                return false;
            }

            float offsetX = (worldSeed & 1023) * 0.173f;
            float offsetY = ((worldSeed >> 10) & 1023) * 0.197f;
            float noise = Mathf.PerlinNoise((x + offsetX) * waterNoiseScale, (y + offsetY) * waterNoiseScale);
            return noise >= waterThreshold;
        }

        public bool IsInsideWaterGrassBuffer(int x, int y)
        {
            int radius = Mathf.Max(0, waterGrassBufferRadius);
            if (!generateWater || radius <= 0 || ShouldGenerateWater(x, y))
            {
                return false;
            }

            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    if (offsetX == 0 && offsetY == 0)
                    {
                        continue;
                    }

                    if (ShouldGenerateWater(x + offsetX, y + offsetY))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public float GetForestNoise(int x, int y)
        {
            float offsetX = (worldSeed & 2047) * 0.127f;
            float offsetY = ((worldSeed >> 11) & 2047) * 0.139f;
            return Mathf.PerlinNoise((x + offsetX) * forestClusterScale, (y + offsetY) * forestClusterScale);
        }

        public Color GetGrassTint(int x, int y)
        {
            if (!enableGrassNoiseGradient)
            {
                return Color.white;
            }

            float seedOffsetX = (worldSeed & 4095) * 0.091f;
            float seedOffsetY = ((worldSeed >> 12) & 4095) * 0.107f;
            float lowFrequency = Mathf.PerlinNoise(
                (x + seedOffsetX) * grassLowFrequencyScale,
                (y + seedOffsetY) * grassLowFrequencyScale);
            float highFrequency = Mathf.PerlinNoise(
                (x - seedOffsetY + 37.1f) * grassHighFrequencyScale,
                (y + seedOffsetX - 19.7f) * grassHighFrequencyScale);

            float palettePosition = Mathf.Clamp01(
                lowFrequency + (highFrequency - 0.5f) * grassHighFrequencyStrength);
            Color paletteColor;
            if (palettePosition < 0.5f)
            {
                float blend = Mathf.SmoothStep(0f, 1f, palettePosition * 2f);
                paletteColor = Color.Lerp(grassForestTint, grassBaseTint, blend);
            }
            else
            {
                float blend = Mathf.SmoothStep(0f, 1f, (palettePosition - 0.5f) * 2f);
                paletteColor = Color.Lerp(grassBaseTint, grassMeadowTint, blend);
            }

            Color result = Color.Lerp(Color.white, paletteColor, grassTintStrength);
            result.a = 1f;
            return result;
        }

        public float GetCellRandom01(int x, int y, int salt)
        {
            unchecked
            {
                uint hash = (uint)worldSeed;
                hash ^= (uint)x * 0x8da6b343u;
                hash ^= (uint)y * 0xd8163841u;
                hash ^= (uint)salt * 0xcb1ab31fu;
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                return (hash & 0x00ffffffu) / 16777215f;
            }
        }
    }
}
