using DG.Tweening;
using LoadGame.Map;
using TMPro;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class BuildingQualityFeedbackUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;
        [SerializeField] private RectTransform feedbackRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text qualityText;
        [SerializeField] private TMP_Text probabilityText;
        [SerializeField] private TMPAnimatedRainbowText rainbowTextEffect;
        [SerializeField] private string probabilityPrefix = "Chance ";

        [Header("Positions")]
        [SerializeField] private Vector2 leftPosition = new Vector2(-900f, 0f);
        [SerializeField] private Vector2 centerPosition = Vector2.zero;
        [SerializeField] private Vector2 rightPosition = new Vector2(900f, 0f);

        [Header("Timing")]
        [SerializeField, Min(0f)] private float enterDuration = 0.3f;
        [SerializeField, Min(0f)] private float centerHoldDuration = 1f;
        [SerializeField, Min(0f)] private float exitDuration = 0.3f;
        [SerializeField] private Ease enterEase = Ease.OutCubic;
        [SerializeField] private Ease exitEase = Ease.InCubic;

        [Header("Quality Colors")]
        [SerializeField] private Color silverColor = new Color(0.72f, 0.85f, 1f, 1f);
        [SerializeField] private Color goldColor = new Color(1f, 0.72f, 0.15f, 1f);
        [SerializeField] private Color rainbowFallbackColor = Color.white;

        private Sequence feedbackSequence;
        private bool subscribed;

        private void Awake()
        {
            if (feedbackRoot == null)
            {
                feedbackRoot = transform as RectTransform;
            }

            if (rainbowTextEffect == null && qualityText != null)
            {
                rainbowTextEffect = qualityText.GetComponent<TMPAnimatedRainbowText>();
            }

            if (rainbowTextEffect != null)
            {
                rainbowTextEffect.enabled = false;
            }

            ResetImmediate();
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
            KillTweens();
            ResetImmediate();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<ChunkedGridMapRenderer>();
            }

            if (mapRenderer == null)
            {
                return;
            }

            mapRenderer.BuildingQualityRevealed += Play;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || mapRenderer == null)
            {
                return;
            }

            mapRenderer.BuildingQualityRevealed -= Play;
            subscribed = false;
        }

        private void Play(BuildingInstance instance)
        {
            if (instance == null || instance.Quality == BuildingQuality.Normal
                || feedbackRoot == null || qualityText == null)
            {
                return;
            }

            KillTweens();
            feedbackRoot.anchoredPosition = leftPosition;
            qualityText.text = GetQualityLabel(instance.Quality);
            qualityText.color = GetQualityColor(instance.Quality);
            if (rainbowTextEffect != null)
            {
                rainbowTextEffect.enabled = instance.Quality == BuildingQuality.Rainbow;
            }

            qualityText.ForceMeshUpdate();
            if (probabilityText != null)
            {
                probabilityText.text = $"{probabilityPrefix}{instance.QualityProbability:P0}";
                probabilityText.color = GetQualityColor(instance.Quality);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            feedbackSequence = DOTween.Sequence()
                .Append(feedbackRoot
                    .DOAnchorPos(centerPosition, enterDuration)
                    .SetEase(enterEase))
                .AppendInterval(centerHoldDuration)
                .Append(feedbackRoot
                    .DOAnchorPos(rightPosition, exitDuration)
                    .SetEase(exitEase))
                .OnComplete(ResetImmediate);
        }

        private string GetQualityLabel(BuildingQuality quality)
        {
            switch (quality)
            {
                case BuildingQuality.Silver:
                    return "Silver!";
                case BuildingQuality.Gold:
                    return "Gold!!";
                case BuildingQuality.Rainbow:
                    return "Rainbow!!!";
                default:
                    return string.Empty;
            }
        }

        private Color GetQualityColor(BuildingQuality quality)
        {
            switch (quality)
            {
                case BuildingQuality.Silver:
                    return silverColor;
                case BuildingQuality.Gold:
                    return goldColor;
                case BuildingQuality.Rainbow:
                    return rainbowFallbackColor;
                default:
                    return Color.white;
            }
        }

        private void KillTweens()
        {
            feedbackSequence?.Kill();
            feedbackSequence = null;
        }

        private void ResetImmediate()
        {
            if (rainbowTextEffect != null)
            {
                rainbowTextEffect.enabled = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (feedbackRoot != null)
            {
                feedbackRoot.anchoredPosition = leftPosition;
            }
        }
    }
}
