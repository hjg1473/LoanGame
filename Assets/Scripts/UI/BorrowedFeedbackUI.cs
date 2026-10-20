using DG.Tweening;
using LoadGame.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class BorrowedFeedbackUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private Image flashImage;
        [SerializeField] private TMP_Text borrowedText;

        [Header("Flash")]
        [SerializeField, Range(0f, 1f)] private float flashPeakAlpha = 0.75f;
        [SerializeField, Min(0f)] private float flashFadeDuration = 0.35f;
        [SerializeField] private Ease flashFadeEase = Ease.OutQuad;

        [Header("Borrowed Text")]
        [SerializeField, Min(0f)] private float punchDuration = 0.3f;
        [SerializeField, Min(0f)] private float punchStrength = 0.2f;
        [SerializeField, Min(1)] private int punchVibrato = 8;
        [SerializeField, Range(0f, 1f)] private float punchElasticity = 0.8f;
        [SerializeField, Min(0f)] private float textHoldAfterPunch = 0.2f;
        [SerializeField, Min(0f)] private float textFadeDuration = 0.8f;
        [SerializeField] private Ease textFadeEase = Ease.InQuad;

        private RectTransform borrowedTextRect;
        private Vector3 borrowedTextBaseScale;
        private Sequence feedbackSequence;
        private bool subscribed;

        private void Awake()
        {
            if (borrowedText != null)
            {
                borrowedTextRect = borrowedText.rectTransform;
                borrowedTextBaseScale = borrowedTextRect.localScale;
                borrowedText.raycastTarget = false;
            }

            if (flashImage != null)
            {
                flashImage.raycastTarget = false;
            }

            ResetVisuals();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            feedbackSequence?.Kill();
            feedbackSequence = null;
            ResetVisuals();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (economyManager == null)
            {
                return;
            }

            economyManager.Borrowed += Play;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || economyManager == null)
            {
                return;
            }

            economyManager.Borrowed -= Play;
            subscribed = false;
        }

        private void Play(long borrowedAmount)
        {
            if (borrowedAmount <= 0L)
            {
                return;
            }

            feedbackSequence?.Kill();
            ResetVisuals();

            if (flashImage != null)
            {
                SetGraphicAlpha(flashImage, flashPeakAlpha);
            }

            if (borrowedText != null)
            {
                borrowedText.text = $"BORROWED ${borrowedAmount:N0}";
                SetGraphicAlpha(borrowedText, 1f);
            }

            feedbackSequence = DOTween.Sequence();

            if (flashImage != null)
            {
                feedbackSequence.Join(
                    flashImage
                        .DOFade(0f, flashFadeDuration)
                        .SetEase(flashFadeEase));
            }

            if (borrowedTextRect != null)
            {
                feedbackSequence.Join(
                    borrowedTextRect.DOPunchScale(
                        Vector3.one * punchStrength,
                        punchDuration,
                        punchVibrato,
                        punchElasticity));
            }

            if (borrowedText != null)
            {
                float fadeStartTime = punchDuration + textHoldAfterPunch;
                feedbackSequence.Insert(
                    fadeStartTime,
                    borrowedText
                        .DOFade(0f, textFadeDuration)
                        .SetEase(textFadeEase));
            }

            feedbackSequence.OnComplete(ResetVisuals);
        }

        private void ResetVisuals()
        {
            if (flashImage != null)
            {
                SetGraphicAlpha(flashImage, 0f);
            }

            if (borrowedText != null)
            {
                SetGraphicAlpha(borrowedText, 0f);
            }

            if (borrowedTextRect != null)
            {
                borrowedTextRect.localScale = borrowedTextBaseScale;
            }
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
