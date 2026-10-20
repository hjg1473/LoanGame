using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class BuildingIncomePopupPool : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private TextMeshProUGUI popupTextPrefab;
        [SerializeField] private RectTransform popupContainer;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField, Min(1)] private int initialPoolSize = 12;
        [SerializeField, Min(1)] private int maxVisiblePopups = 30;

        [Header("Presentation")]
        [SerializeField, Min(1f)] private float baseFontSize = 28f;
        [SerializeField] private Color popupColor = new Color(0.35f, 1f, 0.45f, 1f);
        [SerializeField, Min(0f)] private float riseDistance = 55f;
        [SerializeField, Min(0.01f)] private float lifetime = 1.1f;
        [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = 0.55f;
        [SerializeField, Min(0.01f)] private float minimumScale = 1f;
        [SerializeField, Min(0.01f)] private float maximumScale = 1.35f;
        [SerializeField, Min(0.01f)] private float minimumFontMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float maximumFontMultiplier = 1.3f;
        [SerializeField, Min(0.01f)] private float productionForMaximumSize = 400f;

        private readonly Stack<BuildingIncomePopup> availablePopups = new Stack<BuildingIncomePopup>(16);
        private readonly List<BuildingIncomePopup> allPopups = new List<BuildingIncomePopup>(32);
        private int visiblePopupCount;
        private bool initialized;

        private void Awake()
        {
            InitializePool();
        }

        public bool Show(Vector3 worldPosition, Camera worldCamera, long amount, double productionPerSecond)
        {
            if (!initialized)
            {
                InitializePool();
            }

            if (popupContainer == null || worldCamera == null || amount <= 0L
                || visiblePopupCount >= Mathf.Max(1, maxVisiblePopups))
            {
                return false;
            }

            BuildingIncomePopup popup = GetPopup();
            if (popup == null)
            {
                return false;
            }

            Camera canvasCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    popupContainer,
                    screenPosition,
                    canvasCamera,
                    out Vector2 localPosition))
            {
                ReturnPopup(popup);
                return false;
            }

            float sizeFactor = Mathf.Clamp01((float)(productionPerSecond / Mathf.Max(0.01f, productionForMaximumSize)));
            float scale = Mathf.Lerp(minimumScale, maximumScale, sizeFactor);
            float fontSize = baseFontSize * Mathf.Lerp(minimumFontMultiplier, maximumFontMultiplier, sizeFactor);

            visiblePopupCount++;
            popup.Play(
                worldPosition,
                worldCamera,
                canvasCamera,
                popupContainer,
                localPosition,
                amount,
                fontSize,
                scale,
                popupColor,
                riseDistance,
                lifetime,
                fadeStartNormalized);
            return true;
        }

        private void InitializePool()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (popupTextPrefab == null || popupContainer == null)
            {
                Debug.LogWarning("BuildingIncomePopupPool requires a popup prefab and container.", this);
                return;
            }

            int preloadCount = Mathf.Min(Mathf.Max(1, initialPoolSize), Mathf.Max(1, maxVisiblePopups));
            for (int i = 0; i < preloadCount; i++)
            {
                CreatePopup(true);
            }
        }

        private BuildingIncomePopup GetPopup()
        {
            if (availablePopups.Count > 0)
            {
                return availablePopups.Pop();
            }

            return allPopups.Count < Mathf.Max(1, maxVisiblePopups) ? CreatePopup(false) : null;
        }

        private BuildingIncomePopup CreatePopup(bool addToAvailable)
        {
            TextMeshProUGUI label = Instantiate(popupTextPrefab, popupContainer);
            BuildingIncomePopup popup = label.GetComponent<BuildingIncomePopup>();
            if (popup == null)
            {
                popup = label.gameObject.AddComponent<BuildingIncomePopup>();
            }

            popup.Initialize(label, ReturnPopup);
            allPopups.Add(popup);
            if (addToAvailable)
            {
                availablePopups.Push(popup);
            }

            return popup;
        }

        private void ReturnPopup(BuildingIncomePopup popup)
        {
            if (popup == null)
            {
                return;
            }

            popup.ResetForPool();
            availablePopups.Push(popup);
            visiblePopupCount = Mathf.Max(0, visiblePopupCount - 1);
        }
    }
}
