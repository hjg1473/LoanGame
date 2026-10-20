using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoadGame.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class BtnPointer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float tweenDuration = 0.08f;
        [SerializeField] private Ease pressEase = Ease.OutQuad;
        [SerializeField] private Ease releaseEase = Ease.OutBack;

        private RectTransform targetRect;
        private Tween scaleTween;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage != null)
            {
                targetRect = targetImage.rectTransform;
            }
        }

        private void OnDisable()
        {
            scaleTween?.Kill();
            scaleTween = null;

            if (targetRect != null)
            {
                targetRect.localScale = Vector3.one * normalScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TweenScale(pressedScale, pressEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            TweenScale(normalScale, releaseEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerPress == gameObject)
            {
                TweenScale(normalScale, releaseEase);
            }
        }

        private void TweenScale(float scale, Ease ease)
        {
            if (targetRect == null)
            {
                return;
            }

            scaleTween?.Kill();
            scaleTween = targetRect
                .DOScale(scale, Mathf.Max(0f, tweenDuration))
                .SetEase(ease)
                .SetUpdate(isIndependentUpdate: true);
        }
    }
}
