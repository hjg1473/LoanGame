using System;
using TMPro;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class BuildingIncomePopup : MonoBehaviour
    {
        private TextMeshProUGUI textLabel;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Action<BuildingIncomePopup> returnToPool;
        private Camera worldCamera;
        private Camera canvasCamera;
        private RectTransform popupContainer;
        private Vector3 worldPosition;
        private float riseDistance;
        private float lifetime;
        private float fadeStartNormalized;
        private float elapsed;
        private bool isPlaying;

        public void Initialize(TextMeshProUGUI label, Action<BuildingIncomePopup> onFinished)
        {
            textLabel = label;
            rectTransform = label.rectTransform;
            canvasGroup = label.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = label.gameObject.AddComponent<CanvasGroup>();
            }

            returnToPool = onFinished;
            ResetForPool();
        }

        public void Play(
            Vector3 popupWorldPosition,
            Camera popupWorldCamera,
            Camera popupCanvasCamera,
            RectTransform container,
            Vector2 initialAnchoredPosition,
            long amount,
            float fontSize,
            float scale,
            Color color,
            float popupRiseDistance,
            float popupLifetime,
            float popupFadeStartNormalized)
        {
            gameObject.SetActive(true);
            worldPosition = popupWorldPosition;
            worldCamera = popupWorldCamera;
            canvasCamera = popupCanvasCamera;
            popupContainer = container;
            riseDistance = Mathf.Max(0f, popupRiseDistance);
            lifetime = Mathf.Max(0.01f, popupLifetime);
            fadeStartNormalized = Mathf.Clamp01(popupFadeStartNormalized);
            elapsed = 0f;
            isPlaying = true;

            textLabel.text = $"+${amount:N0}";
            textLabel.fontSize = Mathf.Max(1f, fontSize);
            textLabel.color = color;
            canvasGroup.alpha = 1f;
            rectTransform.anchoredPosition = initialAnchoredPosition;
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0f);
            rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            rectTransform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / lifetime);
            float easedMovement = 1f - (1f - normalizedTime) * (1f - normalizedTime);
            UpdateTrackedPosition(easedMovement);
            canvasGroup.alpha = normalizedTime <= fadeStartNormalized
                ? 1f
                : 1f - Mathf.InverseLerp(fadeStartNormalized, 1f, normalizedTime);

            if (elapsed >= lifetime)
            {
                isPlaying = false;
                returnToPool?.Invoke(this);
            }
        }

        public void ResetForPool()
        {
            isPlaying = false;
            worldCamera = null;
            canvasCamera = null;
            popupContainer = null;
            worldPosition = Vector3.zero;
            riseDistance = 0f;
            lifetime = 0f;
            elapsed = 0f;
            if (textLabel != null)
            {
                textLabel.text = string.Empty;
                textLabel.fontSize = 1f;
                textLabel.color = Color.white;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }

            gameObject.SetActive(false);
        }

        private void UpdateTrackedPosition(float movementProgress)
        {
            if (worldCamera == null || popupContainer == null)
            {
                return;
            }

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    popupContainer,
                    screenPosition,
                    canvasCamera,
                    out Vector2 localPosition))
            {
                rectTransform.anchoredPosition = localPosition + Vector2.up * riseDistance * movementProgress;
            }
        }
    }
}
