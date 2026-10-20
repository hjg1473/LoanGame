using UnityEngine;

namespace LoadGame.Upgrades
{
    public enum UpgradeEffectType
    {
        WorkPower,
        CityEfficiency
    }

    [CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "Game/Upgrade Definition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        public string upgradeId = "upgrade";
        public string displayName = "Upgrade";
        [TextArea] public string description;
        public Sprite icon;
        public int sortOrder;
        public UpgradeEffectType effectType;

        [Header("Level")]
        [Min(1)] public int startingLevel = 1;
        [Min(1)] public int maxLevel = 999;

        [Header("Cost")]
        [Min(1)] public long baseCost = 100L;
        [Min(1.001f)] public double costGrowth = 1.18d;

        [Header("Effect")]
        [Tooltip("Additional multiplier per level. 0.1 means +10% per level.")]
        [Min(0f)] public double multiplierPerLevel = 0.1d;
    }
}
