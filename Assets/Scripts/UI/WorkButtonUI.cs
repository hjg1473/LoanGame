using DG.Tweening;
using LoadGame.Audio;
using LoadGame.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class WorkButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private Image targetImage;
        [SerializeField] private RectTransform popupSpawnPosition;
        [SerializeField] private WorkRewardCalculator rewardCalculator;
        [SerializeField] private WorkPopupPool popupPool;

        [Header("Sprites")]
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite pressedSprite;

        [Header("Feedback")]
        [SerializeField] private float autoReleaseDelay = 0.08f;
        [SerializeField] private float holdDelay = 0.35f;
        [SerializeField] private float holdInterval = 0.12f;
        private RectTransform rectTransform;
        private Tween autoReleaseTween;
        private bool isPointerDown;
        private bool isHolding;
        private float holdTimer;
        private float holdRepeatTimer;

        private void Awake()
        {
            rectTransform = transform as RectTransform;

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (rewardCalculator == null)
            {
                rewardCalculator = GetComponent<WorkRewardCalculator>();
            }

            if (popupPool == null)
            {
                popupPool = GetComponent<WorkPopupPool>();
            }

            SetScaleOne();
            SetNormalVisual();
        }

        private void OnDisable()
        {
            autoReleaseTween?.Kill();
            autoReleaseTween = null;
            isPointerDown = false;
            isHolding = false;
            holdTimer = 0f;
            holdRepeatTimer = 0f;
            SetNormalVisual();
            SetScaleOne();
        }

        private void Update()
        {
            if (!isPointerDown)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (!isHolding)
            {
                holdTimer += deltaTime;
                if (holdTimer < GetSafeHoldDelay())
                {
                    return;
                }

                isHolding = true;
                holdRepeatTimer = 0f;
                ExecuteWork();
                return;
            }

            holdRepeatTimer += deltaTime;
            float safeHoldInterval = GetSafeHoldInterval();
            while (holdRepeatTimer >= safeHoldInterval)
            {
                holdRepeatTimer -= safeHoldInterval;
                ExecuteWork();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Press();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        private void Press()
        {
            isPointerDown = true;
            isHolding = false;
            holdTimer = 0f;
            holdRepeatTimer = 0f;
            autoReleaseTween?.Kill();
            autoReleaseTween = null;

            ExecuteWork();
            SetPressedVisual();
            SetScaleOne();
        }

        private void ExecuteWork()
        {
            if (rewardCalculator == null)
            {
                Debug.LogWarning("WorkButtonUI requires a WorkRewardCalculator.", this);
                return;
            }

            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            WorkRewardResult reward = rewardCalculator.Roll();
            if (economyManager != null)
            {
                economyManager.AddCash(reward.Amount);
            }

            SoundManager.Instance?.PlaySFX(SoundId.Work);
            if (popupPool != null && popupSpawnPosition != null)
            {
                popupPool.ShowWorkPopup(popupSpawnPosition, reward.Amount, reward.Tier);
            }
        }

        private void Release()
        {
            if (!isPointerDown && autoReleaseTween == null)
            {
                return;
            }

            isPointerDown = false;
            isHolding = false;
            holdTimer = 0f;
            holdRepeatTimer = 0f;
            autoReleaseTween?.Kill();

            if (autoReleaseDelay > 0f)
            {
                autoReleaseTween = DOVirtual.DelayedCall(autoReleaseDelay, FinishRelease, ignoreTimeScale: false);
                return;
            }

            FinishRelease();
        }

        private void FinishRelease()
        {
            autoReleaseTween = null;
            if (isPointerDown)
            {
                return;
            }

            SetNormalVisual();
            SetScaleOne();
        }

        private void SetPressedVisual()
        {
            if (targetImage != null && pressedSprite != null)
            {
                targetImage.sprite = pressedSprite;
            }
        }

        private void SetNormalVisual()
        {
            if (targetImage != null && normalSprite != null)
            {
                targetImage.sprite = normalSprite;
            }
        }

        private void SetScaleOne()
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.localScale = Vector3.one;
        }

        private float GetSafeHoldDelay()
        {
            return holdDelay > 0f ? holdDelay : 0.35f;
        }

        private float GetSafeHoldInterval()
        {
            return holdInterval > 0f ? holdInterval : 0.12f;
        }
    }
}
