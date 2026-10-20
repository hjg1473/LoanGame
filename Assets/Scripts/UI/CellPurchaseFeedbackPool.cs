using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class CellPurchaseFeedbackPool : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private TextMeshProUGUI popupTextPrefab;
        [SerializeField] private RectTransform popupContainer;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField, Min(1)] private int initialPoolSize = 4;
        [SerializeField, Min(1)] private int maxPoolSize = 8;

        [Header("Presentation")]
        [SerializeField] private Color textColor = new Color(1f, 0.35f, 0.3f, 1f);
        [SerializeField, Min(1f)] private float fontSize = 28f;
        [SerializeField, Min(0f)] private float riseDistance = 65f;
        [SerializeField, Min(0.01f)] private float lifetime = 1.2f;
        [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = 0.45f;

        private readonly Stack<CellPurchaseFeedbackPopup> available = new Stack<CellPurchaseFeedbackPopup>(8);
        private readonly List<CellPurchaseFeedbackPopup> all = new List<CellPurchaseFeedbackPopup>(8);
        private bool initialized;

        private void Awake()
        {
            InitializePool();
        }

        public void Show(string message, Vector3 worldPosition, Camera worldCamera)
        {
            if (!initialized)
            {
                InitializePool();
            }

            if (string.IsNullOrEmpty(message) || popupContainer == null || worldCamera == null)
            {
                return;
            }

            CellPurchaseFeedbackPopup popup = GetPopup();
            if (popup == null)
            {
                return;
            }

            Camera canvasCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
            popup.Play(
                message, worldPosition, worldCamera, canvasCamera, popupContainer,
                textColor, fontSize, riseDistance, lifetime, fadeStartNormalized);
        }

        private void InitializePool()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (popupTextPrefab == null || popupContainer == null)
            {
                Debug.LogWarning("CellPurchaseFeedbackPool requires a popup prefab and container.", this);
                return;
            }

            int count = Mathf.Min(Mathf.Max(1, initialPoolSize), Mathf.Max(1, maxPoolSize));
            for (int i = 0; i < count; i++)
            {
                CreatePopup(true);
            }
        }

        private CellPurchaseFeedbackPopup GetPopup()
        {
            if (available.Count > 0)
            {
                return available.Pop();
            }

            return all.Count < Mathf.Max(1, maxPoolSize) ? CreatePopup(false) : null;
        }

        private CellPurchaseFeedbackPopup CreatePopup(bool addToPool)
        {
            TextMeshProUGUI label = Instantiate(popupTextPrefab, popupContainer);
            CellPurchaseFeedbackPopup popup = label.GetComponent<CellPurchaseFeedbackPopup>();
            if (popup == null)
            {
                popup = label.gameObject.AddComponent<CellPurchaseFeedbackPopup>();
            }

            popup.Initialize(label, ReturnPopup);
            all.Add(popup);
            if (addToPool)
            {
                available.Push(popup);
            }

            return popup;
        }

        private void ReturnPopup(CellPurchaseFeedbackPopup popup)
        {
            if (popup == null)
            {
                return;
            }

            popup.ResetForPool();
            available.Push(popup);
        }
    }
}
