using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LoadGame.Map
{
    public sealed class BuildingOptionView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text productionText;
        [SerializeField] private TMP_Text maintenanceText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button buildButton;
        [SerializeField] private CanvasGroup canvasGroup;
        [Header("Availability Colors")]
        [SerializeField] private Image[] imageTintTargets;
        [SerializeField] private TMP_Text[] textTintTargets;
        [FormerlySerializedAs("availableColor")]
        [SerializeField] private Color availableImageColor = Color.white;
        [SerializeField] private Color availableTextColor = Color.white;
        [FormerlySerializedAs("unavailableColor")]
        [SerializeField] private Color unavailableImageColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color unavailableTextColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private float unavailableAlpha = 0.65f;

        private int buildingIndex;
        private Action<int> clicked;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (imageTintTargets == null || imageTintTargets.Length == 0)
            {
                imageTintTargets = GetComponentsInChildren<Image>(true);
            }

            if (textTintTargets == null || textTintTargets.Length == 0)
            {
                textTintTargets = GetComponentsInChildren<TMP_Text>(true);
            }

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
                string roleSummary = GetRoleSummary(definition);
                productionText.text = maintenanceText == null && definition.maintenancePerSecond > 0d
                    ? $"{roleSummary}  -${definition.maintenancePerSecond:N0}/s"
                    : roleSummary;
            }

            if (maintenanceText != null)
            {
                maintenanceText.text = definition.maintenancePerSecond > 0d
                    ? $"-${definition.maintenancePerSecond:N0}/s"
                    : string.Empty;
            }

            if (priceText != null)
            {
                priceText.text = $"{definition.price:N0}";
            }

            if (buildButton != null)
            {
                buildButton.interactable = interactable;
            }

            ApplyAvailability(interactable);
            gameObject.SetActive(true);
        }

        private static string GetRoleSummary(BuildingDefinition definition)
        {
            if (definition.IsRoad)
            {
                return "AUTO-CONNECT ROAD";
            }

            switch (definition.buildingRole)
            {
                case BuildingRole.Residential:
                    return $"MAX POP +{definition.populationCapacity:N0}";
                case BuildingRole.Workplace:
                    return $"WORKERS {definition.requiredWorkers:N0}  +{definition.cashPerSecond:N0}/s";
                case BuildingRole.Leisure:
                    return $"LEISURE +{definition.leisureCapacity:N0}";
                default:
                    return definition.cashPerSecond > 0d ? $"+{definition.cashPerSecond:N0}/s" : string.Empty;
            }
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

        private void ApplyAvailability(bool available)
        {
            Color imageColor = available ? availableImageColor : unavailableImageColor;
            if (imageTintTargets != null)
            {
                for (int i = 0; i < imageTintTargets.Length; i++)
                {
                    if (imageTintTargets[i] != null)
                    {
                        imageTintTargets[i].color = imageColor;
                    }
                }
            }

            Color textColor = available ? availableTextColor : unavailableTextColor;
            if (textTintTargets != null)
            {
                for (int i = 0; i < textTintTargets.Length; i++)
                {
                    if (textTintTargets[i] != null)
                    {
                        textTintTargets[i].color = textColor;
                    }
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = available ? 1f : unavailableAlpha;
            }
        }
    }
}
