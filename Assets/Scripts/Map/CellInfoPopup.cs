using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.Map
{
    public sealed class CellInfoPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject buildingListRoot;
        [SerializeField] private BuildingOptionView[] buildingOptionViews;
        [SerializeField] private GridCellSelectionController selectionController;

        private Sequence sequence;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;

        public void Initialize(
            CanvasGroup popupCanvasGroup,
            RectTransform popupPanel,
            TMP_Text popupTitleText,
            TMP_Text popupBodyText,
            Button popupCloseButton,
            GridCellSelectionController controller)
        {
            canvasGroup = popupCanvasGroup;
            panel = popupPanel;
            titleText = popupTitleText;
            bodyText = popupBodyText;
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

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            panel.localScale = Vector3.one * 0.92f;
            HideBuildingOptions();
        }

        private void OnBuyClicked()
        {
            selectionController?.TryBuySelectedCell();
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
            if (titleText == null || bodyText == null || canvasGroup == null || panel == null)
            {
                Debug.LogError("CellInfoPopup references are not fully assigned.", this);
                return;
            }

            titleText.text = $"Cell {cell.x}, {cell.y}";
            bodyText.text = $"x: {cell.x}\ny: {cell.y}\ntype: {cell.type}\nowned: {cell.owned}\nbuildingId: {GetBuildingText(cell)}";
            bool showPurchase = cell.CellType == GridCellType.Empty && !cell.owned;
            bool showBuildList = cell.CellType == GridCellType.Empty
                && cell.owned
                && string.IsNullOrEmpty(cell.buildingId)
                && buildingDefinitions != null
                && buildingDefinitions.Length > 0;

            if (priceText != null)
            {
                priceText.gameObject.SetActive(showPurchase);
                priceText.text = showPurchase ? $"Price: {landPrice:N0}" : string.Empty;
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(showPurchase);
                buyButton.interactable = canBuy;
            }

            ShowBuildingOptions(showBuildList, buildingDefinitions, canBuild, iconResolver);

            sequence?.Kill();
            canvasGroup.blocksRaycasts = true;
            panel.localScale = Vector3.one * 0.92f;

            sequence = DOTween.Sequence()
                .Join(canvasGroup.DOFade(1f, 0.16f))
                .Join(panel.DOScale(1f, 0.16f).SetEase(Ease.OutBack));
        }

        public void ShowInstalledBuilding(GridCellData cell, BuildingDefinition buildingDefinition)
        {
            if (titleText == null || bodyText == null || canvasGroup == null || panel == null)
            {
                Debug.LogError("CellInfoPopup references are not fully assigned.", this);
                return;
            }

            titleText.text = buildingDefinition.displayName;
            bodyText.text = $"x: {cell.x}\ny: {cell.y}\nbuildingId: {cell.buildingId}\nProduction/sec: +{buildingDefinition.cashPerSecond:N0}";

            if (priceText != null)
            {
                priceText.gameObject.SetActive(false);
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
            }

            HideBuildingOptions();
            sequence?.Kill();
            canvasGroup.blocksRaycasts = true;
            panel.localScale = Vector3.one * 0.92f;

            sequence = DOTween.Sequence()
                .Join(canvasGroup.DOFade(1f, 0.16f))
                .Join(panel.DOScale(1f, 0.16f).SetEase(Ease.OutBack));
        }

        public void Hide()
        {
            selectionController?.ClearSelection();

            sequence?.Kill();
            if (canvasGroup == null || panel == null)
            {
                return;
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
            }

            if (priceText != null)
            {
                priceText.gameObject.SetActive(false);
            }

            HideBuildingOptions();

            canvasGroup.blocksRaycasts = false;

            sequence = DOTween.Sequence()
                .Join(canvasGroup.DOFade(0f, 0.12f))
                .Join(panel.DOScale(0.92f, 0.12f).SetEase(Ease.InQuad));
        }

        private static string GetBuildingText(GridCellData cell)
        {
            return string.IsNullOrEmpty(cell.buildingId) ? "none" : cell.buildingId;
        }

        private void ShowBuildingOptions(
            bool show,
            BuildingDefinition[] buildingDefinitions,
            bool canBuild,
            Func<BuildingDefinition, Sprite> iconResolver)
        {
            if (buildingListRoot != null)
            {
                buildingListRoot.SetActive(show);
            }

            if (!show || buildingOptionViews == null)
            {
                HideBuildingOptions();
                return;
            }

            int count = Mathf.Min(buildingDefinitions.Length, buildingOptionViews.Length);
            for (int i = 0; i < count; i++)
            {
                Sprite icon = iconResolver == null ? null : iconResolver(buildingDefinitions[i]);
                buildingOptionViews[i].Bind(i, buildingDefinitions[i], icon, canBuild, OnBuildingOptionClicked);
            }

            for (int i = count; i < buildingOptionViews.Length; i++)
            {
                buildingOptionViews[i].Hide();
            }
        }

        private void HideBuildingOptions()
        {
            if (buildingListRoot != null)
            {
                buildingListRoot.SetActive(false);
            }

            if (buildingOptionViews == null)
            {
                return;
            }

            for (int i = 0; i < buildingOptionViews.Length; i++)
            {
                if (buildingOptionViews[i] != null)
                {
                    buildingOptionViews[i].Hide();
                }
            }
        }

        private void OnBuildingOptionClicked(int buildingIndex)
        {
            selectionController?.TryBuildSelectedCell(buildingIndex);
        }
    }
}
