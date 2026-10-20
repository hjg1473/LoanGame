using System;
using LoadGame.Upgrades;
using UnityEngine;

namespace LoadGame.UI
{
    [Serializable]
    public sealed class WorkRewardTier
    {
        public string id = "normal";
        [Min(0f)] public float weight = 90f;
        [Min(0f)] public double multiplier = 1d;
        [Min(0.01f)] public float fontSizeMultiplier = 1f;
        [Min(0.01f)] public float popupScale = 1f;
        [Min(0.01f)] public float jumpPowerMultiplier = 1f;
        [Min(0.01f)] public float lifetimeMultiplier = 1f;
        public Color popupColor = Color.white;
    }

    public readonly struct WorkRewardResult
    {
        public WorkRewardResult(double amount, WorkRewardTier tier)
        {
            Amount = amount;
            Tier = tier;
        }

        public double Amount { get; }
        public WorkRewardTier Tier { get; }
    }

    public sealed class WorkRewardCalculator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private double baseWorkValue = 10d;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private WorkRewardTier[] rewardTiers =
        {
            new WorkRewardTier
            {
                id = "normal", weight = 90f, multiplier = 1d,
                fontSizeMultiplier = 1f, popupScale = 1f,
                jumpPowerMultiplier = 1f, lifetimeMultiplier = 1f,
                popupColor = Color.white
            },
            new WorkRewardTier
            {
                id = "lucky", weight = 9f, multiplier = 10d,
                fontSizeMultiplier = 1.25f, popupScale = 1.2f,
                jumpPowerMultiplier = 1.1f, lifetimeMultiplier = 1.05f,
                popupColor = new Color(1f, 0.86f, 0.25f, 1f)
            },
            new WorkRewardTier
            {
                id = "jackpot", weight = 1f, multiplier = 100d,
                fontSizeMultiplier = 1.6f, popupScale = 1.5f,
                jumpPowerMultiplier = 1.25f, lifetimeMultiplier = 1.15f,
                popupColor = new Color(1f, 0.35f, 0.15f, 1f)
            }
        };

        private readonly WorkRewardTier fallbackTier = new WorkRewardTier();

        public double BaseWorkValue => baseWorkValue;

        public WorkRewardResult Roll()
        {
            if (upgradeManager == null)
            {
                upgradeManager = UpgradeManager.Instance;
            }

            WorkRewardTier tier = SelectTier();
            double workPowerMultiplier = upgradeManager == null ? 1d : upgradeManager.GetWorkPowerMultiplier();
            double amount = Math.Round(
                Math.Max(0d, baseWorkValue) * workPowerMultiplier * Math.Max(0d, tier.multiplier),
                MidpointRounding.AwayFromZero);
            return new WorkRewardResult(amount, tier);
        }

        public void SetBaseWorkValue(double value)
        {
            baseWorkValue = Math.Max(0d, value);
        }

        private WorkRewardTier SelectTier()
        {
            float totalWeight = 0f;
            if (rewardTiers != null)
            {
                for (int i = 0; i < rewardTiers.Length; i++)
                {
                    if (rewardTiers[i] != null && rewardTiers[i].weight > 0f)
                    {
                        totalWeight += rewardTiers[i].weight;
                    }
                }
            }

            if (totalWeight <= 0f)
            {
                return fallbackTier;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < rewardTiers.Length; i++)
            {
                WorkRewardTier tier = rewardTiers[i];
                if (tier == null || tier.weight <= 0f)
                {
                    continue;
                }

                roll -= tier.weight;
                if (roll <= 0f)
                {
                    return tier;
                }
            }

            return fallbackTier;
        }
    }
}
