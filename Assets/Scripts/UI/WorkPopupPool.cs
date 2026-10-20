using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class WorkPopupPool : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private TextMeshProUGUI popupTextPrefab;
        [SerializeField] private RectTransform popupContainer;
        [SerializeField, Min(1)] private int initialPoolSize = 20;
        [SerializeField] private bool autoExpand = true;

        [Header("Presentation")]
        [SerializeField, Min(1f)] private float baseFontSize = 36f;
        [SerializeField] private Vector2 horizontalVelocityRange = new Vector2(-80f, 80f);
        [SerializeField] private Vector2 verticalVelocityRange = new Vector2(220f, 300f);
        [SerializeField, Min(0f)] private float gravity = 600f;
        [SerializeField] private Vector2 lifetimeRange = new Vector2(0.9f, 1.2f);
        [SerializeField] private Vector2 fadeStartNormalizedRange = new Vector2(0.45f, 0.6f);
        [SerializeField, Min(0f)] private float maxRotationSpeed = 12f;

        private readonly Stack<WorkPopupText> availablePopups = new Stack<WorkPopupText>(20);
        private bool initialized;

        private void Awake()
        {
            InitializePool();
        }

        public void ShowWorkPopup(RectTransform spawnPosition, double amount, WorkRewardTier tier)
        {
            if (!initialized)
            {
                InitializePool();
            }

            if (tier == null || spawnPosition == null)
            {
                return;
            }

            WorkPopupText popup = GetPopup();
            if (popup == null)
            {
                return;
            }

            popup.Play(
                spawnPosition.position,
                amount,
                tier,
                baseFontSize,
                Mathf.Min(horizontalVelocityRange.x, horizontalVelocityRange.y),
                Mathf.Max(horizontalVelocityRange.x, horizontalVelocityRange.y),
                Mathf.Min(verticalVelocityRange.x, verticalVelocityRange.y),
                Mathf.Max(verticalVelocityRange.x, verticalVelocityRange.y),
                gravity,
                Mathf.Min(lifetimeRange.x, lifetimeRange.y),
                Mathf.Max(lifetimeRange.x, lifetimeRange.y),
                Mathf.Min(fadeStartNormalizedRange.x, fadeStartNormalizedRange.y),
                Mathf.Max(fadeStartNormalizedRange.x, fadeStartNormalizedRange.y),
                maxRotationSpeed);
        }

        private void InitializePool()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (popupTextPrefab == null)
            {
                Debug.LogWarning("WorkPopupPool requires a Popup Text Prefab.", this);
                return;
            }

            for (int i = 0; i < Mathf.Max(1, initialPoolSize); i++)
            {
                CreatePopup();
            }
        }

        private WorkPopupText GetPopup()
        {
            if (availablePopups.Count > 0)
            {
                return availablePopups.Pop();
            }

            return autoExpand ? CreatePopup(false) : null;
        }

        private WorkPopupText CreatePopup(bool addToPool = true)
        {
            RectTransform parent = popupContainer != null ? popupContainer : transform.parent as RectTransform;
            TextMeshProUGUI label = Instantiate(popupTextPrefab, parent);
            WorkPopupText popup = label.GetComponent<WorkPopupText>();
            if (popup == null)
            {
                popup = label.gameObject.AddComponent<WorkPopupText>();
            }

            popup.Initialize(label, ReturnPopup);
            if (addToPool)
            {
                availablePopups.Push(popup);
            }

            return popup;
        }

        private void ReturnPopup(WorkPopupText popup)
        {
            popup.ResetForPool();
            availablePopups.Push(popup);
        }
    }
}
