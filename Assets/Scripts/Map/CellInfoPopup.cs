using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LoadGame.Economy;
using LoadGame.Upgrades;

namespace LoadGame.Map
{
    public sealed class CellInfoPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [Header("Bottom Sheet")]
        [SerializeField] private Vector2 openPosition = Vector2.zero;
        [SerializeField] private Vector2 closedPosition = new Vector2(0f, -700f);
        [SerializeField, Min(0f)] private float openDuration = 0.25f;
        [SerializeField, Min(0f)] private float closeDuration = 0.2f;
        [SerializeField] private Ease openEase = Ease.OutCubic;
        [SerializeField] private Ease closeEase = Ease.InCubic;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject landPurchaseRoot;
        [SerializeField] private TMP_Text landPriceText;
        [SerializeField] private GameObject buildingPurchaseRoot;
        [SerializeField] private GameObject buildingListRoot;
        [SerializeField] private Transform buildingOptionContainer;
        [SerializeField] private BuildingOptionView buildingOptionPrefab;
        [Header("Building Star Sections")]
        [SerializeField] private GameObject oneStarSectionRoot;
        [SerializeField] private Transform oneStarContainer;
        [SerializeField] private GameObject twoStarSectionRoot;
        [SerializeField] private Transform twoStarContainer;
        [SerializeField] private GameObject threeStarSectionRoot;
        [SerializeField] private Transform threeStarContainer;
        [Tooltip("Parent RectTransform that vertically arranges the 1/2/3-star sections.")]
        [SerializeField] private RectTransform starSectionsLayoutRoot;
        [Header("Building Role Filters")]
        [SerializeField] private Button residentialFilterButton;
        [SerializeField] private Button workplaceFilterButton;
        [SerializeField] private Button otherFilterButton;
        [SerializeField] private Color selectedFilterColor = Color.white;
        [SerializeField] private Color unselectedFilterColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private GameObject buildingInfoRoot;
        [SerializeField] private TMP_Text buildingIncomeText;
        [SerializeField] private Image buildingIconImage;
        [SerializeField] private Button removeRoadButton;
        [SerializeField] private GridCellSelectionController selectionController;

        private readonly List<BuildingOptionView> pooledBuildingOptionViews = new List<BuildingOptionView>(8);
        private readonly List<int> filteredBuildingIndices = new List<int>(16);
        private Sequence sequence;
        private BuildingDefinition[] currentBuildingDefinitions;
        private Func<BuildingDefinition, bool> currentCanBuildResolver;
        private Func<BuildingDefinition, Sprite> currentIconResolver;
        private BuildingRoleFilter selectedBuildingRoleFilter = BuildingRoleFilter.Residential;
        private bool buildingOptionsVisible;

        private enum BuildingRoleFilter
        {
            Residential,
            Workplace,
            Other
        }

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;

        public void Initialize(
            CanvasGroup popupCanvasGroup,
            RectTransform popupPanel,
            Button popupCloseButton,
            GridCellSelectionController controller)
        {
            canvasGroup = popupCanvasGroup;
            panel = popupPanel;
            closeButton = popupCloseButton;
            selectionController = controller;
            Configure();
        }

        public void SetSelectionController(GridCellSelectionController controller)
        {
            selectionController = controller;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
        {
            return IsVisible
                && panel != null
                && RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition, eventCamera);
        }

        private void Awake()
        {
            Configure();
        }

        private void Configure()
        {
            if (canvasGroup == null || panel == null || closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(OnBuyClicked);
                buyButton.onClick.AddListener(OnBuyClicked);
            }

            if (removeRoadButton != null)
            {
                removeRoadButton.onClick.RemoveListener(OnRemoveRoadClicked);
                removeRoadButton.onClick.AddListener(OnRemoveRoadClicked);
            }

            ConfigureFilterButton(residentialFilterButton, ShowResidentialBuildings);
            ConfigureFilterButton(workplaceFilterButton, ShowWorkplaceBuildings);
            ConfigureFilterButton(otherFilterButton, ShowOtherBuildings);
            RefreshFilterButtonVisuals();

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            panel.localScale = Vector3.one;
            panel.anchoredPosition = closedPosition;
            HideStateRoots();
        }

        private void OnBuyClicked()
        {
            selectionController?.TryBuySelectedCell();
        }

        private void OnRemoveRoadClicked()
        {
            selectionController?.TryRemoveSelectedObject();
        }

        public void Show(GridCellData cell)
        {
            Show(cell, 0L, false, null, false);
        }

        public void Show(GridCellData cell, long landPrice, bool canBuy)
        {
            Show(cell, landPrice, canBuy, null, false);
        }

        public void Show(GridCellData cell, long landPrice, bool canBuy, BuildingDefinition buildingDefinition, bool canBuild)
        {
            BuildingDefinition[] definitions = buildingDefinition == null ? null : new[] { buildingDefinition };
            Show(cell, landPrice, canBuy, definitions, canBuild, null);
        }

        public void Show(
            GridCellData cell,
            long landPrice,
            bool canBuy,
            BuildingDefinition[] buildingDefinitions,
            bool canBuild,
            Func<BuildingDefinition, Sprite> iconResolver)
        {
            Show(cell, landPrice, canBuy, buildingDefinitions, definition => canBuild, iconResolver);
        }

        public void Show(
            GridCellData cell,
            long landPrice,
            bool canBuy,
            BuildingDefinition[] buildingDefinitions,
            Func<BuildingDefinition, bool> canBuildResolver,
            Func<BuildingDefinition, Sprite> iconResolver)
        {
            if (canvasGroup == null || panel == null)
            {
                Debug.LogError("CellInfoPopup references are not fully assigned.", this);
                return;
            }

            bool showPurchase = cell.CellType == GridCellType.Empty && !cell.owned;
            bool showBuildList = cell.CellType == GridCellType.Empty
                && cell.owned
                && string.IsNullOrEmpty(cell.buildingId)
                && buildingDefinitions != null
                && buildingDefinitions.Length > 0;

            SetRootActive(landPurchaseRoot, showPurchase);
            SetRootActive(buildingPurchaseRoot, showBuildList);
            SetRootActive(buildingInfoRoot, false);
            SetButtonActive(removeRoadButton, false);

            SetText(landPriceText, showPurchase ? $"{landPrice:N0}" : string.Empty);
            SetTextActive(landPriceText, showPurchase);

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(showPurchase);
                buyButton.interactable = canBuy;
            }

            ShowBuildingOptions(showBuildList, buildingDefinitions, canBuildResolver, iconResolver);

            OpenBottomSheet();
        }

        public void ShowInstalledBuilding(GridCellData cell, BuildingDefinition buildingDefinition)
        {
            if (canvasGroup == null || panel == null)
            {
                Debug.LogError("CellInfoPopup references are not fully assigned.", this);
                return;
            }

            ShowInstalledBuilding(cell, buildingDefinition, null);
        }

        public void ShowRoad(GridCellData cell)
        {
            if (canvasGroup == null || panel == null)
            {
                Debug.LogError("CellInfoPopup references are not fully assigned.", this);
                return;
            }

            SetText(buildingIncomeText, string.Empty);
            SetTextActive(buildingIncomeText, false);
            SetTextActive(landPriceText, false);
            SetRootActive(landPurchaseRoot, false);
            SetRootActive(buildingPurchaseRoot, false);
            SetRootActive(buildingInfoRoot, true);
            SetButtonActive(removeRoadButton, true);

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
            }

            if (buildingIconImage != null)
            {
                buildingIconImage.sprite = null;
                buildingIconImage.enabled = false;
            }

            HideBuildingOptions();
            OpenBottomSheet();
        }

        public void ShowInstalledBuilding(GridCellData cell, BuildingDefinition buildingDefinition, Sprite icon)
        {
            ShowInstalledBuilding(cell, buildingDefinition, buildingDefinition == null ? 0d : buildingDefinition.cashPerSecond, icon);
        }

        public void ShowInstalledBuilding(GridCellData cell, BuildingInstance instance, Sprite icon)
        {
            double production = instance == null ? 0d : instance.GetProductionPerSecond();
            if (instance != null && instance.Definition != null)
            {
                PopulationManager populationManager = PopulationManager.Instance;
                UpgradeManager upgradeManager = UpgradeManager.Instance;
                production *= populationManager == null
                    ? 1d
                    : populationManager.GetProductionMultiplier(instance.Definition);
                production *= upgradeManager == null
                    ? 1d
                    : upgradeManager.GetCityEfficiencyMultiplier();
            }

            ShowInstalledBuilding(
                cell,
                instance == null ? null : instance.Definition,
                production,
                icon);
        }

        private void ShowInstalledBuilding(
            GridCellData cell,
            BuildingDefinition buildingDefinition,
            double productionPerSecond,
            Sprite icon)
        {
            if (canvasGroup == null || panel == null)
            {
                Debug.LogError("CellInfoPopup references are not fully assigned.", this);
                return;
            }

            SetText(buildingIncomeText, $"+{productionPerSecond:N0}/sec");
            SetTextActive(landPriceText, false);
            SetRootActive(landPurchaseRoot, false);
            SetRootActive(buildingPurchaseRoot, false);
            SetRootActive(buildingInfoRoot, true);
            SetButtonActive(removeRoadButton, buildingDefinition != null);

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
            }

            if (buildingIconImage != null)
            {
                buildingIconImage.sprite = icon;
                buildingIconImage.enabled = icon != null;
            }

            HideBuildingOptions();
            OpenBottomSheet();
        }

        public void Hide()
        {
            selectionController?.ClearSelection();

            sequence?.Kill();
            if (canvasGroup == null || panel == null)
            {
                return;
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            sequence = DOTween.Sequence()
                .Join(canvasGroup.DOFade(0f, Mathf.Max(0f, closeDuration * 0.75f)))
                .Join(panel.DOAnchorPos(closedPosition, closeDuration).SetEase(closeEase))
                .OnComplete(() =>
                {
                    if (buyButton != null)
                    {
                        buyButton.gameObject.SetActive(false);
                    }

                    HideStateRoots();
                });
        }

        private void OpenBottomSheet()
        {
            sequence?.Kill();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            panel.localScale = Vector3.one;

            sequence = DOTween.Sequence()
                .Join(canvasGroup.DOFade(1f, Mathf.Max(0f, openDuration * 0.75f)))
                .Join(panel.DOAnchorPos(openPosition, openDuration).SetEase(openEase));
        }

        private void ShowBuildingOptions(
            bool show,
            BuildingDefinition[] buildingDefinitions,
            Func<BuildingDefinition, bool> canBuildResolver,
            Func<BuildingDefinition, Sprite> iconResolver)
        {
            if (buildingListRoot != null)
            {
                buildingListRoot.SetActive(show);
            }

            buildingOptionsVisible = show;
            currentBuildingDefinitions = buildingDefinitions;
            currentCanBuildResolver = canBuildResolver;
            currentIconResolver = iconResolver;

            if (!show || buildingDefinitions == null || buildingDefinitions.Length == 0)
            {
                HideBuildingOptions();
                return;
            }

            RefreshBuildingOptions();
        }

        private void RefreshBuildingOptions()
        {
            if (!buildingOptionsVisible || currentBuildingDefinitions == null)
            {
                return;
            }

            filteredBuildingIndices.Clear();
            for (int i = 0; i < currentBuildingDefinitions.Length; i++)
            {
                BuildingDefinition definition = currentBuildingDefinitions[i];
                if (definition != null && MatchesSelectedRole(definition))
                {
                    filteredBuildingIndices.Add(i);
                }
            }

            filteredBuildingIndices.Sort(CompareBuildingIndices);
            int displayedCount = 0;
            int oneStarCount = 0;
            int twoStarCount = 0;
            int threeStarCount = 0;
            for (int i = 0; i < filteredBuildingIndices.Count; i++)
            {
                int buildingIndex = filteredBuildingIndices[i];
                BuildingDefinition definition = currentBuildingDefinitions[buildingIndex];

                BuildingOptionView optionView = GetOrCreateBuildingOptionView(displayedCount);
                if (optionView == null)
                {
                    continue;
                }

                int starSection = Mathf.Clamp(definition.starRating, 1, 3);
                Transform sectionContainer = GetStarContainer(starSection);
                if (sectionContainer != null && optionView.transform.parent != sectionContainer)
                {
                    optionView.transform.SetParent(sectionContainer, worldPositionStays: false);
                }

                int siblingIndex = starSection == 1
                    ? oneStarCount
                    : starSection == 2
                        ? twoStarCount
                        : threeStarCount;
                optionView.transform.SetSiblingIndex(siblingIndex);

                Sprite icon = currentIconResolver == null ? null : currentIconResolver(definition);
                bool canBuild = currentCanBuildResolver == null || currentCanBuildResolver(definition);
                optionView.Bind(buildingIndex, definition, icon, canBuild, OnBuildingOptionClicked);
                displayedCount++;

                switch (starSection)
                {
                    case 1:
                        oneStarCount++;
                        break;
                    case 2:
                        twoStarCount++;
                        break;
                    default:
                        threeStarCount++;
                        break;
                }
            }

            for (int i = displayedCount; i < pooledBuildingOptionViews.Count; i++)
            {
                if (pooledBuildingOptionViews[i] != null)
                {
                    pooledBuildingOptionViews[i].Hide();
                }
            }

            SetRootActive(oneStarSectionRoot, oneStarCount > 0);
            SetRootActive(twoStarSectionRoot, twoStarCount > 0);
            SetRootActive(threeStarSectionRoot, threeStarCount > 0);
            RebuildStarSectionLayouts();
        }

        private void HideBuildingOptions()
        {
            buildingOptionsVisible = false;
            if (buildingListRoot != null)
            {
                buildingListRoot.SetActive(false);
            }

            for (int i = 0; i < pooledBuildingOptionViews.Count; i++)
            {
                if (pooledBuildingOptionViews[i] != null)
                {
                    pooledBuildingOptionViews[i].Hide();
                }
            }


            SetRootActive(oneStarSectionRoot, false);
            SetRootActive(twoStarSectionRoot, false);
            SetRootActive(threeStarSectionRoot, false);
            RebuildStarSectionLayouts();
        }

        private Transform GetStarContainer(int starRating)
        {
            switch (starRating)
            {
                case 1:
                    return oneStarContainer != null ? oneStarContainer : buildingOptionContainer;
                case 2:
                    return twoStarContainer != null ? twoStarContainer : buildingOptionContainer;
                default:
                    return threeStarContainer != null ? threeStarContainer : buildingOptionContainer;
            }
        }

        private void RebuildStarSectionLayouts()
        {
            Canvas.ForceUpdateCanvases();
            RebuildLayout(oneStarContainer as RectTransform);
            RebuildLayout(twoStarContainer as RectTransform);
            RebuildLayout(threeStarContainer as RectTransform);
            RebuildLayout(oneStarSectionRoot == null ? null : oneStarSectionRoot.transform as RectTransform);
            RebuildLayout(twoStarSectionRoot == null ? null : twoStarSectionRoot.transform as RectTransform);
            RebuildLayout(threeStarSectionRoot == null ? null : threeStarSectionRoot.transform as RectTransform);
            RebuildLayout(starSectionsLayoutRoot);
        }

        private static void RebuildLayout(RectTransform target)
        {
            if (target != null && target.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(target);
            }
        }

        private BuildingOptionView GetOrCreateBuildingOptionView(int index)
        {
            while (pooledBuildingOptionViews.Count <= index)
            {
                BuildingOptionView template = buildingOptionPrefab;
                if (template == null)
                {
                    Debug.LogWarning("Building option prefab is required for dynamic building options.", this);
                    return null;
                }

                Transform parent = buildingOptionContainer;
                if (parent == null && buildingListRoot != null)
                {
                    parent = buildingListRoot.transform;
                }

                BuildingOptionView createdView = Instantiate(template, parent);
                createdView.Hide();
                pooledBuildingOptionViews.Add(createdView);
            }

            return pooledBuildingOptionViews[index];
        }

        private void OnBuildingOptionClicked(int buildingIndex)
        {
            selectionController?.TryBuildSelectedCell(buildingIndex);
        }

        private bool MatchesSelectedRole(BuildingDefinition definition)
        {
            switch (selectedBuildingRoleFilter)
            {
                case BuildingRoleFilter.Residential:
                    return definition.buildingRole == BuildingRole.Residential;
                case BuildingRoleFilter.Workplace:
                    return definition.buildingRole == BuildingRole.Workplace;
                default:
                    return definition.buildingRole != BuildingRole.Residential
                        && definition.buildingRole != BuildingRole.Workplace;
            }
        }

        private int CompareBuildingIndices(int leftIndex, int rightIndex)
        {
            int leftStars = Mathf.Max(1, currentBuildingDefinitions[leftIndex].starRating);
            int rightStars = Mathf.Max(1, currentBuildingDefinitions[rightIndex].starRating);
            int starComparison = leftStars.CompareTo(rightStars);
            return starComparison != 0 ? starComparison : leftIndex.CompareTo(rightIndex);
        }

        private void ShowResidentialBuildings()
        {
            SetBuildingRoleFilter(BuildingRoleFilter.Residential);
        }

        private void ShowWorkplaceBuildings()
        {
            SetBuildingRoleFilter(BuildingRoleFilter.Workplace);
        }

        private void ShowOtherBuildings()
        {
            SetBuildingRoleFilter(BuildingRoleFilter.Other);
        }

        private void SetBuildingRoleFilter(BuildingRoleFilter filter)
        {
            selectedBuildingRoleFilter = filter;
            RefreshFilterButtonVisuals();
            RefreshBuildingOptions();
        }

        private static void ConfigureFilterButton(Button button, UnityEngine.Events.UnityAction listener)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(listener);
            button.onClick.AddListener(listener);
        }

        private void RefreshFilterButtonVisuals()
        {
            SetFilterButtonColor(residentialFilterButton, selectedBuildingRoleFilter == BuildingRoleFilter.Residential);
            SetFilterButtonColor(workplaceFilterButton, selectedBuildingRoleFilter == BuildingRoleFilter.Workplace);
            SetFilterButtonColor(otherFilterButton, selectedBuildingRoleFilter == BuildingRoleFilter.Other);
        }

        private void SetFilterButtonColor(Button button, bool selected)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? selectedFilterColor : unselectedFilterColor;
            }
        }

        private void HideStateRoots()
        {
            SetRootActive(landPurchaseRoot, false);
            SetRootActive(buildingPurchaseRoot, false);
            SetRootActive(buildingInfoRoot, false);
            SetButtonActive(removeRoadButton, false);
            SetTextActive(landPriceText, false);
            HideBuildingOptions();
        }

        private static void SetRootActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetTextActive(TMP_Text target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        private static void SetButtonActive(Button target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
                target.interactable = active;
            }
        }
    }
}
