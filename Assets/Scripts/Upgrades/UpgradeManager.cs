using System;
using System.Collections.Generic;
using LoadGame.Economy;
using UnityEngine;

namespace LoadGame.Upgrades
{
    public sealed class UpgradeManager : MonoBehaviour
    {
        [SerializeField] private UpgradeDatabase database;
        [SerializeField] private EconomyManager economyManager;

        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();
        private readonly List<UpgradeDefinition> sortedUpgrades = new List<UpgradeDefinition>();

        public static UpgradeManager Instance { get; private set; }
        public event Action<UpgradeDefinition, int> UpgradeChanged;
        public IReadOnlyList<UpgradeDefinition> Upgrades => sortedUpgrades;

        public List<UpgradeSaveState> CaptureSaveState()
        {
            List<UpgradeSaveState> states = new List<UpgradeSaveState>(levels.Count);
            foreach (KeyValuePair<string, int> pair in levels)
            {
                states.Add(new UpgradeSaveState { upgradeId = pair.Key, level = pair.Value });
            }
            return states;
        }

        public void RestoreSaveState(List<UpgradeSaveState> states)
        {
            if (states == null)
            {
                return;
            }
            for (int i = 0; i < states.Count; i++)
            {
                UpgradeSaveState state = states[i];
                if (state != null && levels.ContainsKey(state.upgradeId))
                {
                    levels[state.upgradeId] = Mathf.Max(1, state.level);
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            BuildRuntimeState();
        }

        private void Start()
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            economyManager?.SetUpgradeManager(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public int GetLevel(UpgradeDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.upgradeId))
            {
                return 0;
            }

            return levels.TryGetValue(definition.upgradeId, out int level)
                ? level
                : Mathf.Max(1, definition.startingLevel);
        }

        public long GetNextCost(UpgradeDefinition definition)
        {
            if (definition == null || GetLevel(definition) >= GetMaxLevel(definition))
            {
                return 0L;
            }

            int purchasedLevels = Mathf.Max(0, GetLevel(definition) - Mathf.Max(1, definition.startingLevel));
            double cost = Math.Max(1d, definition.baseCost) * Math.Pow(Math.Max(1.001d, definition.costGrowth), purchasedLevels);
            return cost >= long.MaxValue ? long.MaxValue : Math.Max(1L, (long)Math.Ceiling(cost));
        }

        public bool CanPurchase(UpgradeDefinition definition)
        {
            if (definition == null || economyManager == null || economyManager.IsBankrupt())
            {
                return false;
            }

            return GetNextCost(definition) > 0L;
        }

        public bool TryPurchase(UpgradeDefinition definition)
        {
            if (!CanPurchase(definition))
            {
                return false;
            }

            long cost = GetNextCost(definition);
            economyManager.Pay(cost);

            int nextLevel = Mathf.Min(GetMaxLevel(definition), GetLevel(definition) + 1);
            levels[definition.upgradeId] = nextLevel;
            UpgradeChanged?.Invoke(definition, nextLevel);
            return true;
        }

        public double GetWorkPowerMultiplier()
        {
            return GetEffectMultiplier(UpgradeEffectType.WorkPower);
        }

        public double GetCityEfficiencyMultiplier()
        {
            return GetEffectMultiplier(UpgradeEffectType.CityEfficiency);
        }

        private double GetEffectMultiplier(UpgradeEffectType effectType)
        {
            double bonus = 0d;
            for (int i = 0; i < sortedUpgrades.Count; i++)
            {
                UpgradeDefinition definition = sortedUpgrades[i];
                if (definition == null || definition.effectType != effectType)
                {
                    continue;
                }

                int gainedLevels = Mathf.Max(0, GetLevel(definition) - Mathf.Max(1, definition.startingLevel));
                bonus += gainedLevels * Math.Max(0d, definition.multiplierPerLevel);
            }

            return Math.Max(1d, 1d + bonus);
        }

        private void BuildRuntimeState()
        {
            levels.Clear();
            sortedUpgrades.Clear();
            if (database == null || database.upgrades == null)
            {
                return;
            }

            for (int i = 0; i < database.upgrades.Count; i++)
            {
                UpgradeDefinition definition = database.upgrades[i];
                if (definition == null || string.IsNullOrEmpty(definition.upgradeId) || levels.ContainsKey(definition.upgradeId))
                {
                    continue;
                }

                int startingLevel = Mathf.Clamp(definition.startingLevel, 1, GetMaxLevel(definition));
                levels.Add(definition.upgradeId, startingLevel);
                sortedUpgrades.Add(definition);
            }

            sortedUpgrades.Sort((left, right) => left.sortOrder.CompareTo(right.sortOrder));
        }

        private static int GetMaxLevel(UpgradeDefinition definition)
        {
            return Mathf.Max(1, definition.maxLevel);
        }
    }
}
