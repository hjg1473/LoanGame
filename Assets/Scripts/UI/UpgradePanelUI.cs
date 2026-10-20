using System.Collections.Generic;
using LoadGame.Economy;
using LoadGame.Upgrades;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class UpgradePanelUI : MonoBehaviour
    {
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private UpgradeListItemUI itemPrefab;
        [SerializeField] private RectTransform content;

        private readonly List<UpgradeListItemUI> items = new List<UpgradeListItemUI>();

        private void Start()
        {
            if (upgradeManager == null)
            {
                upgradeManager = UpgradeManager.Instance;
            }

            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            BuildList();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void BuildList()
        {
            if (upgradeManager == null || itemPrefab == null || content == null)
            {
                return;
            }

            IReadOnlyList<UpgradeDefinition> definitions = upgradeManager.Upgrades;
            for (int i = 0; i < definitions.Count; i++)
            {
                UpgradeListItemUI item = Instantiate(itemPrefab, content);
                item.Bind(definitions[i], upgradeManager);
                items.Add(item);
            }
        }

        private void Subscribe()
        {
            if (upgradeManager != null)
            {
                upgradeManager.UpgradeChanged += OnUpgradeChanged;
            }

            if (economyManager != null)
            {
                economyManager.CashChanged += OnCashChanged;
            }
        }

        private void Unsubscribe()
        {
            if (upgradeManager != null)
            {
                upgradeManager.UpgradeChanged -= OnUpgradeChanged;
            }

            if (economyManager != null)
            {
                economyManager.CashChanged -= OnCashChanged;
            }
        }

        private void OnUpgradeChanged(UpgradeDefinition definition, int level)
        {
            RefreshItems();
        }

        private void OnCashChanged(long cash)
        {
            RefreshItems();
        }

        private void RefreshItems()
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    items[i].Refresh();
                }
            }
        }
    }
}
