using System;
using TMPro;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class WorkPopupText : MonoBehaviour
    {
        private TextMeshProUGUI textLabel;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Action<WorkPopupText> returnToPool;
        private Vector2 velocity;
        private float gravity;
        private float lifetime;
        private float fadeStartNormalized;
        private float elapsed;
        private float targetScale;
        private float angularVelocity;
        private bool isPlaying;

        public void Initialize(TextMeshProUGUI label, Action<WorkPopupText> onFinished)
        {
            textLabel = label;
            rectTransform = label.rectTransform;
            canvasGroup = label.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = label.gameObject.AddComponent<CanvasGroup>();
            }

            returnToPool = onFinished;
            gameObject.SetActive(false);
        }

        public void Play(
            Vector3 worldPosition,
            double amount,
            WorkRewardTier tier,
            float baseFontSize,
            float horizontalVelocityMin,
            float horizontalVelocityMax,
            float verticalVelocityMin,
            float verticalVelocityMax,
            float popupGravity,
            float lifetimeMin,
            float lifetimeMax,
            float fadeStartMin,
            float fadeStartMax,
            float rotationSpeed)
        {
            gameObject.SetActive(true);

            float jumpMultiplier = Mathf.Max(0.01f, tier.jumpPowerMultiplier);
            velocity.x = UnityEngine.Random.Range(horizontalVelocityMin, horizontalVelocityMax) * jumpMultiplier;
            velocity.y = UnityEngine.Random.Range(verticalVelocityMin, verticalVelocityMax) * jumpMultiplier;
            gravity = Mathf.Max(0f, popupGravity);
            lifetime = Mathf.Max(0.01f, UnityEngine.Random.Range(lifetimeMin, lifetimeMax)
                * Mathf.Max(0.01f, tier.lifetimeMultiplier));
            fadeStartNormalized = Mathf.Clamp01(UnityEngine.Random.Range(fadeStartMin, fadeStartMax));
            angularVelocity = UnityEngine.Random.Range(-rotationSpeed, rotationSpeed);
            elapsed = 0f;
            targetScale = Mathf.Max(0.01f, tier.popupScale);
            isPlaying = true;

            textLabel.text = $"+${amount:N0}";
            textLabel.fontSize = baseFontSize * Mathf.Max(0.01f, tier.fontSizeMultiplier);
            textLabel.color = tier.popupColor;
            canvasGroup.alpha = 1f;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.position = worldPosition;
            rectTransform.localScale = Vector3.one * targetScale;
            rectTransform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            velocity.y -= gravity * deltaTime;
            rectTransform.anchoredPosition += velocity * deltaTime;
            rectTransform.Rotate(0f, 0f, angularVelocity * deltaTime);

            float normalizedTime = Mathf.Clamp01(elapsed / lifetime);
            canvasGroup.alpha = normalizedTime <= fadeStartNormalized
                ? 1f
                : 1f - Mathf.InverseLerp(fadeStartNormalized, 1f, normalizedTime);

            float punchScale = EvaluatePunchScale(normalizedTime);
            rectTransform.localScale = Vector3.one * targetScale * punchScale;

            if (elapsed >= lifetime)
            {
                Finish();
            }
        }

        private void Finish()
        {
            if (!isPlaying)
            {
                return;
            }

            isPlaying = false;
            returnToPool?.Invoke(this);
        }

        private static float EvaluatePunchScale(float normalizedTime)
        {
            if (normalizedTime < 0.05f)
            {
                return Mathf.Lerp(0.7f, 1.15f, normalizedTime / 0.05f);
            }

            if (normalizedTime < 0.12f)
            {
                return Mathf.Lerp(1.15f, 1f, (normalizedTime - 0.05f) / 0.07f);
            }

            return 1f;
        }

        public void ResetForPool()
        {
            isPlaying = false;
            velocity = Vector2.zero;
            gravity = 0f;
            lifetime = 0f;
            elapsed = 0f;
            angularVelocity = 0f;
            textLabel.text = string.Empty;
            textLabel.fontSize = 1f;
            textLabel.color = Color.white;
            canvasGroup.alpha = 1f;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            gameObject.SetActive(false);
        }
    }
}
