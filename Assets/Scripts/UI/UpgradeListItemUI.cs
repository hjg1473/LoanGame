using LoadGame.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class UpgradeListItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button purchaseButton;

        private UpgradeDefinition definition;
        private UpgradeManager manager;

        private void Awake()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
            }
        }

        private void OnDestroy()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
            }
        }

        public void Bind(UpgradeDefinition upgrade, UpgradeManager upgradeManager)
        {
            definition = upgrade;
            manager = upgradeManager;
            Refresh();
        }

        public void Refresh()
        {
            if (definition == null || manager == null)
            {
                return;
            }

            int level = manager.GetLevel(definition);
            bool isMax = level >= Mathf.Max(1, definition.maxLevel);
            bool canPurchase = !isMax && manager.CanPurchase(definition);

            if (iconImage != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.enabled = definition.icon != null;
            }

            SetText(nameText, definition.displayName);
            SetText(descriptionText, definition.description);
            SetText(levelText, $"LV. {level:N0} / {Mathf.Max(1, definition.maxLevel):N0}");
            SetText(effectText, $"+{GetCurrentBonusPercent(level):N0}%");
            SetText(costText, isMax ? "MAX" : $"${manager.GetNextCost(definition):N0}");

            if (purchaseButton != null)
            {
                purchaseButton.interactable = canPurchase;
            }

        }

        private void OnPurchaseClicked()
        {
            if (manager != null && manager.TryPurchase(definition))
            {
                Refresh();
            }
        }

        private double GetCurrentBonusPercent(int level)
        {
            int gainedLevels = Mathf.Max(0, level - Mathf.Max(1, definition.startingLevel));
            return gainedLevels * definition.multiplierPerLevel * 100d;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
