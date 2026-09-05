using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.Map
{
    public sealed class BuildingOptionView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text productionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button buildButton;

        private int buildingIndex;
        private Action<int> clicked;

        private void Awake()
        {
            if (buildButton != null)
            {
                buildButton.onClick.RemoveListener(OnBuildClicked);
                buildButton.onClick.AddListener(OnBuildClicked);
            }
        }

        public void Bind(int index, BuildingDefinition definition, Sprite icon, bool interactable, Action<int> onClicked)
        {
            buildingIndex = index;
            clicked = onClicked;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
            {
                nameText.text = definition.displayName;
            }

            if (productionText != null)
            {
                productionText.text = $"+{definition.cashPerSecond:N0}/sec";
            }

            if (priceText != null)
            {
                priceText.text = $"{definition.price:N0}";
            }

            if (buildButton != null)
            {
                buildButton.interactable = interactable;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            clicked = null;
        }

        private void OnBuildClicked()
        {
            clicked?.Invoke(buildingIndex);
        }
    }
}
