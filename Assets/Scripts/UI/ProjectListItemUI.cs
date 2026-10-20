using System;
using System.Collections.Generic;
using LoadGame.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class ProjectListItemUI : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image badgeImage;
        [SerializeField] private Image lockIconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color availableBadgeColor = new Color(0.25f, 0.75f, 0.45f, 1f);
        [SerializeField] private Color activeBadgeColor = new Color(0.25f, 0.55f, 1f, 1f);
        [SerializeField] private Color completedBadgeColor = new Color(1f, 0.78f, 0.25f, 1f);
        [SerializeField] private Color lockedBadgeColor = new Color(0.28f, 0.28f, 0.28f, 1f);
        [SerializeField] private float lockedAlpha = 0.58f;

        private DevelopmentProjectRuntime project;
        private Action<DevelopmentProjectRuntime> clicked;

        public void Initialize(Image background, Image icon, Image badge, Image lockIcon, TMP_Text projectName, TMP_Text description, TMP_Text status, TMP_Text summary, Button clickButton, CanvasGroup group)
        {
            backgroundImage = background;
            iconImage = icon;
            badgeImage = badge;
            lockIconImage = lockIcon;
            nameText = projectName;
            descriptionText = description;
            statusText = status;
            summaryText = summary;
            button = clickButton;
            canvasGroup = group;

            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
            }
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }
        }

        public void Render(DevelopmentProjectRuntime runtime, DevelopmentProjectManager manager, Action<DevelopmentProjectRuntime> onClicked)
        {
            project = runtime;
            clicked = onClicked;

            ProjectDefinition definition = runtime == null ? null : runtime.Definition;
            if (definition == null)
            {
                SetText(nameText, "Project unavailable");
                SetText(descriptionText, string.Empty);
                SetText(statusText, string.Empty);
                SetText(summaryText, string.Empty);
                return;
            }

            bool isLocked = runtime.Status == ProjectStatus.Locked;
            SetText(nameText, definition.displayName);
            SetText(descriptionText, definition.description);
            SetText(statusText, GetStatusLabel(runtime.Status));
            SetText(summaryText, BuildSummary(definition, manager));

            if (canvasGroup != null)
            {
                canvasGroup.alpha = isLocked ? lockedAlpha : 1f;
            }

            if (lockIconImage != null)
            {
                lockIconImage.gameObject.SetActive(isLocked);
            }

            SetGraphicColor(backgroundImage, isLocked ? lockedColor : normalColor);
            SetGraphicColor(badgeImage, GetBadgeColor(runtime.Status));
            if (iconImage != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.enabled = definition.icon != null;
            }
        }

        private void OnClicked()
        {
            clicked?.Invoke(project);
        }

        private static string BuildSummary(ProjectDefinition definition, DevelopmentProjectManager manager)
        {
            if (definition.requiredCityValue > 0d)
            {
                return $"City Value ${definition.requiredCityValue:N0}";
            }

            List<ProjectBuildingRequirement> requiredBuildings = definition.requiredBuildings;
            if (requiredBuildings != null && requiredBuildings.Count > 0 && requiredBuildings[0] != null)
            {
                string displayName = manager == null
                    ? GetRequirementName(requiredBuildings[0])
                    : requiredBuildings[0].building != null ? requiredBuildings[0].building.displayName : manager.GetBuildingDisplayName(requiredBuildings[0].buildingType);
                return $"{displayName} {requiredBuildings[0].requiredCount:N0}";
            }

            return "No requirements";
        }

        private static string GetRequirementName(ProjectBuildingRequirement requirement)
        {
            return requirement.building != null ? requirement.building.displayName : requirement.buildingType.ToString();
        }

        private Color GetBadgeColor(ProjectStatus state)
        {
            switch (state)
            {
                case ProjectStatus.Active:
                    return activeBadgeColor;
                case ProjectStatus.Completed:
                    return completedBadgeColor;
                case ProjectStatus.Locked:
                    return lockedBadgeColor;
                default:
                    return availableBadgeColor;
            }
        }

        private static string GetStatusLabel(ProjectStatus state)
        {
            return state == ProjectStatus.Completed ? "COMPLETED" : state.ToString().ToUpperInvariant();
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetGraphicColor(Graphic target, Color color)
        {
            if (target != null)
            {
                target.color = color;
            }
        }
    }
}
