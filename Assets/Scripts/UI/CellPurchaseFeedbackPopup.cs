using System;
using TMPro;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class CellPurchaseFeedbackPopup : MonoBehaviour
    {
        private TextMeshProUGUI label;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Action<CellPurchaseFeedbackPopup> returnToPool;
        private RectTransform container;
        private Camera worldCamera;
        private Camera canvasCamera;
        private Vector3 worldPosition;
        private float riseDistance;
        private float lifetime;
        private float fadeStartNormalized;
        private float elapsed;
        private bool isPlaying;

        public void Initialize(TextMeshProUGUI textLabel, Action<CellPurchaseFeedbackPopup> onFinished)
        {
            label = textLabel;
            rectTransform = textLabel.rectTransform;
            canvasGroup = textLabel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = textLabel.gameObject.AddComponent<CanvasGroup>();
            }

            returnToPool = onFinished;
            ResetForPool();
        }

        public void Play(
            string message,
            Vector3 popupWorldPosition,
            Camera popupWorldCamera,
            Camera popupCanvasCamera,
            RectTransform popupContainer,
            Color color,
            float fontSize,
            float popupRiseDistance,
            float popupLifetime,
            float popupFadeStartNormalized)
        {
            gameObject.SetActive(true);
            label.text = message;
            label.color = color;
            label.fontSize = Mathf.Max(1f, fontSize);
            canvasGroup.alpha = 1f;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            worldPosition = popupWorldPosition;
            worldCamera = popupWorldCamera;
            canvasCamera = popupCanvasCamera;
            container = popupContainer;
            riseDistance = Mathf.Max(0f, popupRiseDistance);
            lifetime = Mathf.Max(0.01f, popupLifetime);
            fadeStartNormalized = Mathf.Clamp01(popupFadeStartNormalized);
            elapsed = 0f;
            isPlaying = true;
            UpdatePosition(0f);
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / lifetime);
            float movement = 1f - (1f - normalizedTime) * (1f - normalizedTime);
            UpdatePosition(movement);
            canvasGroup.alpha = normalizedTime <= fadeStartNormalized
                ? 1f
                : 1f - Mathf.InverseLerp(fadeStartNormalized, 1f, normalizedTime);

            if (elapsed >= lifetime)
            {
                returnToPool?.Invoke(this);
            }
        }

        public void ResetForPool()
        {
            isPlaying = false;
            container = null;
            worldCamera = null;
            canvasCamera = null;
            worldPosition = Vector3.zero;
            elapsed = 0f;

            if (label != null)
            {
                label.text = string.Empty;
                label.color = Color.white;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = Quaternion.identity;
            }

            gameObject.SetActive(false);
        }

        private void UpdatePosition(float movement)
        {
            if (worldCamera == null || container == null)
            {
                return;
            }

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    container, screenPosition, canvasCamera, out Vector2 localPosition))
            {
                rectTransform.anchoredPosition = localPosition + Vector2.up * riseDistance * movement;
            }
        }
    }
}
