using DG.Tweening;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class PhoneAppPanel : MonoBehaviour
    {
        [SerializeField] private string appId = "app";
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.18f;
        [SerializeField] private bool disableObjectWhenClosed;

        private Tween fadeTween;

        public string AppId => appId;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnDisable()
        {
            fadeTween?.Kill();
            fadeTween = null;
        }

        public void OpenInstant()
        {
            IsOpen = true;
            fadeTween?.Kill();
            gameObject.SetActive(true);
            SetCanvasGroup(1f, true);
        }

        public void CloseInstant()
        {
            IsOpen = false;
            fadeTween?.Kill();
            gameObject.SetActive(true);
            SetCanvasGroup(0f, false);
            if (disableObjectWhenClosed)
            {
                gameObject.SetActive(false);
            }
        }

        public void Open()
        {
            IsOpen = true;
            fadeTween?.Kill();
            gameObject.SetActive(true);
            SetCanvasGroup(canvasGroup.alpha, false);
            fadeTween = canvasGroup.DOFade(1f, GetSafeFadeDuration())
                .SetEase(Ease.OutQuad)
                .OnComplete(() => SetCanvasGroup(1f, true));
        }

        public void Close()
        {
            IsOpen = false;
            fadeTween?.Kill();
            SetCanvasGroup(canvasGroup.alpha, false);
            fadeTween = canvasGroup.DOFade(0f, GetSafeFadeDuration())
                .SetEase(Ease.InQuad)
                .OnComplete(FinishClose);
        }

        private void FinishClose()
        {
            gameObject.SetActive(true);
            SetCanvasGroup(0f, false);
            if (disableObjectWhenClosed)
            {
                gameObject.SetActive(false);
            }
        }

        private void SetCanvasGroup(float alpha, bool interactable)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = alpha;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }

        private float GetSafeFadeDuration()
        {
            return fadeDuration > 0f ? fadeDuration : 0.18f;
        }
    }
}
