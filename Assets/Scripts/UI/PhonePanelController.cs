using DG.Tweening;
using LoadGame.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class PhonePanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button openButton;
        [SerializeField] private RectTransform openButtonIcon;
        [SerializeField] private RectTransform phonePanel;
        [SerializeField] private Button closeBackgroundButton;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private PhoneAppManager appManager;

        [Header("Phone Open")]
        [SerializeField] private float closedScale = 0.15f;
        [SerializeField] private float openedScale = 1f;
        [SerializeField] private float scaleDuration = 1f;
        [SerializeField] private float contentFadeDuration = 0.5f;
        [SerializeField] private Ease openEase = Ease.OutCubic;
        [SerializeField] private Ease closeEase = Ease.InCubic;
        [SerializeField] private bool startClosed = true;
        [SerializeField] private bool disablePhonePanelWhenClosed;

        [Header("Icon Attention")]
        [SerializeField] private bool playIconAttention = true;
        [SerializeField] private float attentionInterval = 3f;
        [SerializeField] private float attentionDuration = 2f;
        [SerializeField] private float attentionAngle = 10f;
        [SerializeField] private int attentionRoundTrips = 2;

        private Sequence phoneSequence;
        private Sequence iconSequence;
        private RectTransform phoneScaleTarget;
        private bool isOpen;
        private bool isClosing;

        private void Awake()
        {
            if (openButtonIcon == null && openButton != null)
            {
                openButtonIcon = openButton.transform as RectTransform;
            }

            phoneScaleTarget = phonePanel != null ? phonePanel : openButtonIcon;

            if (appManager == null)
            {
                appManager = GetComponentInChildren<PhoneAppManager>(includeInactive: true);
            }

            if (openButton != null)
            {
                openButton.onClick.RemoveListener(OpenPhone);
                openButton.onClick.AddListener(OpenPhone);
            }

            if (closeBackgroundButton != null)
            {
                closeBackgroundButton.onClick.RemoveListener(ClosePhone);
                closeBackgroundButton.onClick.AddListener(ClosePhone);
            }

            if (startClosed)
            {
                SetClosedInstant();
            }
        }

        private void OnEnable()
        {
            StartIconAttention();
        }

        private void OnDisable()
        {
            phoneSequence?.Kill();
            iconSequence?.Kill();
            phoneSequence = null;
            iconSequence = null;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            HandleBackRequest();
        }

        public bool HandleBackRequest()
        {
            if (appManager != null && appManager.TryCloseCurrentApp())
            {
                return true;
            }

            if (isOpen)
            {
                ClosePhone();
                return true;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            Application.Quit();
#endif
            return false;
        }

        public void OpenPhone()
        {
            if (phonePanel == null)
            {
                Debug.LogError("PhonePanelController needs a phone panel RectTransform.", this);
                return;
            }

            isOpen = true;
            isClosing = false;
            SoundManager.Instance?.PlaySFX(SoundId.PhoneOpen);
            iconSequence?.Kill();
            iconSequence = null;
            if (openButtonIcon != null)
            {
                openButtonIcon.localEulerAngles = Vector3.zero;
            }

            phonePanel.gameObject.SetActive(true);
            SetCloseBackgroundActive(true);
            if (appManager != null)
            {
                appManager.OnPhoneOpened();
            }

            if (contentCanvasGroup != null)
            {
                contentCanvasGroup.alpha = 0f;
                contentCanvasGroup.blocksRaycasts = false;
                contentCanvasGroup.interactable = false;
            }

            phoneSequence?.Kill();
            phoneSequence = DOTween.Sequence()
                .Append(phoneScaleTarget.DOScale(openedScale, scaleDuration).SetEase(openEase));

            if (contentCanvasGroup != null)
            {
                phoneSequence.Append(contentCanvasGroup.DOFade(1f, contentFadeDuration).SetEase(Ease.OutQuad));
            }

            phoneSequence.AppendCallback(EnableContentInteraction);
        }

        public void ClosePhone()
        {
            if (phonePanel == null)
            {
                Debug.LogError("PhonePanelController needs a phone panel RectTransform.", this);
                return;
            }

            if (!isOpen || isClosing)
            {
                return;
            }

            isOpen = false;
            isClosing = true;
            SoundManager.Instance?.PlaySFX(SoundId.PhoneClose);
            SetCloseBackgroundActive(false);
            if (appManager != null)
            {
                appManager.OnPhoneClosing();
            }

            if (contentCanvasGroup != null)
            {
                contentCanvasGroup.blocksRaycasts = false;
                contentCanvasGroup.interactable = false;
            }

            phoneSequence?.Kill();
            phoneSequence = DOTween.Sequence();

            if (contentCanvasGroup != null)
            {
                phoneSequence.Append(contentCanvasGroup.DOFade(0f, contentFadeDuration).SetEase(Ease.InQuad));
            }

            phoneSequence
                .Append(phoneScaleTarget.DOScale(closedScale, scaleDuration).SetEase(closeEase))
                .OnComplete(OnCloseComplete);
        }

        public void TogglePhone()
        {
            if (isOpen)
            {
                ClosePhone();
                return;
            }

            OpenPhone();
        }

        private void SetClosedInstant()
        {
            isOpen = false;
            isClosing = false;

            if (phonePanel != null)
            {
                phoneScaleTarget = phonePanel;
                phoneScaleTarget.localScale = Vector3.one * closedScale;
                if (disablePhonePanelWhenClosed)
                {
                    phonePanel.gameObject.SetActive(false);
                }
            }

            if (contentCanvasGroup != null)
            {
                contentCanvasGroup.alpha = 0f;
                contentCanvasGroup.blocksRaycasts = false;
                contentCanvasGroup.interactable = false;
            }

            SetCloseBackgroundActive(false);
        }

        private void OnCloseComplete()
        {
            isClosing = false;

            if (!isOpen && phonePanel != null && disablePhonePanelWhenClosed)
            {
                phonePanel.gameObject.SetActive(false);
            }

            StartIconAttention();
        }

        private void EnableContentInteraction()
        {
            if (contentCanvasGroup == null)
            {
                return;
            }

            contentCanvasGroup.blocksRaycasts = true;
            contentCanvasGroup.interactable = true;
        }

        private void SetCloseBackgroundActive(bool active)
        {
            if (closeBackgroundButton != null)
            {
                closeBackgroundButton.gameObject.SetActive(active);
            }
        }

        private void StartIconAttention()
        {
            iconSequence?.Kill();

            if (!playIconAttention || openButtonIcon == null)
            {
                return;
            }

            openButtonIcon.localEulerAngles = Vector3.zero;

            int segmentCount = Mathf.Max(1, attentionRoundTrips) * 2 + 1;
            float segmentDuration = attentionDuration / segmentCount;

            iconSequence = DOTween.Sequence()
                .SetLoops(-1)
                .AppendInterval(attentionInterval);

            for (int i = 0; i < attentionRoundTrips; i++)
            {
                iconSequence
                    .Append(openButtonIcon.DOLocalRotate(new Vector3(0f, 0f, -attentionAngle), segmentDuration).SetEase(Ease.InOutSine))
                    .Append(openButtonIcon.DOLocalRotate(new Vector3(0f, 0f, attentionAngle), segmentDuration).SetEase(Ease.InOutSine));
            }

            iconSequence.Append(openButtonIcon.DOLocalRotate(Vector3.zero, segmentDuration).SetEase(Ease.InOutSine));
        }
    }
}
