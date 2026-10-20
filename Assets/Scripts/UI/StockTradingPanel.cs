using LoadGame.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class StockTradingPanel : MonoBehaviour
    {
        [SerializeField] private StockMarketManager stockMarketManager;
        [SerializeField] private TMP_Text currentPriceText;
        [SerializeField] private TMP_Text pnlPercentText;
        [SerializeField] private TMP_Text ownedSharesText;
        [SerializeField] private TMP_Text averageBuyPriceText;
        [SerializeField] private TMP_Text stockValueText;
        [SerializeField] private TMP_Text unrealizedPnLText;
        [SerializeField] private TMP_Text orderQuantityText;
        [SerializeField] private TMP_Text orderTotalText;
        [SerializeField] private TMP_Text fairPriceText;
        [SerializeField] private TMP_Text cityValueText;
        [SerializeField] private TMP_Text fairValueBiasText;
        [SerializeField] private TMP_Text priceImpactText;
        [SerializeField] private TMP_Text finalChangeText;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button maxButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button sellButton;
        [SerializeField] private long orderQuantity = 1L;
        [SerializeField] private Color profitColor = new Color(1f, 0.25f, 0.22f, 1f);
        [SerializeField] private Color lossColor = new Color(0.22f, 0.45f, 1f, 1f);
        [SerializeField] private Color neutralColor = Color.white;
        [SerializeField] private bool createMissingUi = true;

        private RectTransform rectTransform;
        private void Awake()
        {
            rectTransform = transform as RectTransform;

            if (createMissingUi)
            {
                CreateMissingUi();
            }
        }

        private void Start()
        {
            if (stockMarketManager == null)
            {
                stockMarketManager = FindFirstObjectByType<StockMarketManager>();
            }

            if (stockMarketManager != null)
            {
                stockMarketManager.StockPriceChanged += OnStockPriceChanged;
                stockMarketManager.StockPositionChanged += Refresh;
            }

            AddListeners();
            Refresh();
        }

        private void OnDisable()
        {
            if (stockMarketManager != null)
            {
                stockMarketManager.StockPriceChanged -= OnStockPriceChanged;
                stockMarketManager.StockPositionChanged -= Refresh;
            }

            RemoveListeners();
        }

        private void DecreaseOrderQuantity()
        {
            orderQuantity = System.Math.Max(1L, orderQuantity - 1L);
            Refresh();
        }

        private void IncreaseOrderQuantity()
        {
            orderQuantity = SafeAddLong(orderQuantity, 1L);
            Refresh();
        }

        private void SetMaxOrderQuantity()
        {
            if (stockMarketManager == null || stockMarketManager.OwnedShares <= 0L)
            {
                return;
            }

            orderQuantity = stockMarketManager.OwnedShares;
            Refresh();
        }

        private void Buy()
        {
            if (stockMarketManager != null)
            {
                stockMarketManager.BuyShares(GetSafeOrderQuantity());
            }
        }

        private void Sell()
        {
            if (stockMarketManager != null)
            {
                stockMarketManager.SellShares(GetSafeOrderQuantity());
            }
        }

        private void OnStockPriceChanged(double price)
        {
            Refresh();
        }

        private void Refresh()
        {
            orderQuantity = GetSafeOrderQuantity();

            if (stockMarketManager == null)
            {
                SetText(currentPriceText, "Price -");
                SetText(pnlPercentText, "PnL -");
                SetText(ownedSharesText, "Shares -");
                SetText(averageBuyPriceText, "Avg -");
                SetText(stockValueText, "Value -");
                SetText(unrealizedPnLText, "PnL -");
                SetText(orderQuantityText, orderQuantity.ToString("#,0"));
                SetText(orderTotalText, "TOTAL -");
                SetText(fairPriceText, "Fair -");
                SetText(cityValueText, "City -");
                SetText(fairValueBiasText, "Fair Bias -");
                SetText(priceImpactText, "Price Impact -");
                SetText(finalChangeText, "Final Change -");
                RefreshButtonStates(false);
                return;
            }

            double pnl = stockMarketManager.UnrealizedPnL;
            double pnlPercent = stockMarketManager.OwnedShares > 0L && stockMarketManager.AverageBuyPrice > 0d
                ? (stockMarketManager.CurrentPrice - stockMarketManager.AverageBuyPrice) / stockMarketManager.AverageBuyPrice
                : 0d;
            Color pnlColor = GetPnlColor(pnl);

            SetText(currentPriceText, FormatMoney(stockMarketManager.CurrentPrice));
            SetText(pnlPercentText, stockMarketManager.OwnedShares > 0L ? $"{FormatSignedMoney(pnl)} ({FormatSignedPercent(pnlPercent)})" : "$0.00");
            SetTextColor(pnlPercentText, pnlColor);
            SetText(ownedSharesText, $"Shares {stockMarketManager.OwnedShares:#,0}");
            SetText(averageBuyPriceText, $"Avg {FormatMoney(stockMarketManager.AverageBuyPrice)}");
            SetText(stockValueText, $"Value {FormatMoney(stockMarketManager.StockValue)}");
            SetText(unrealizedPnLText, $"PnL {FormatSignedMoney(pnl)} ({FormatSignedPercent(pnlPercent)})");
            SetTextColor(unrealizedPnLText, pnlColor);
            SetText(orderQuantityText, orderQuantity.ToString("#,0"));
            SetRichText(orderTotalText, true);
            SetText(orderTotalText, $"<size=32>{orderQuantity:#,0}</size> Shares\nTOTAL <size=32>{FormatMoney(stockMarketManager.CurrentPrice * orderQuantity)}</size>");
            SetText(fairPriceText, $"Fair {FormatMoney(stockMarketManager.FairPrice)}");
            SetText(cityValueText, $"City {FormatMoney(stockMarketManager.CityValue)}");
            SetText(fairValueBiasText, $"Fair Bias {stockMarketManager.FairValueBias:P3}");
            SetText(priceImpactText, $"Price Impact {stockMarketManager.PriceImpact:P3}");
            SetText(finalChangeText, $"Final Change {stockMarketManager.FinalChangeRate:P3}");
            RefreshButtonStates(stockMarketManager.OwnedShares > 0L);
        }

        private void AddListeners()
        {
            AddListener(buyButton, Buy);
            AddListener(sellButton, Sell);
            AddListener(minusButton, DecreaseOrderQuantity);
            AddListener(plusButton, IncreaseOrderQuantity);
            AddListener(maxButton, SetMaxOrderQuantity);
        }

        private void RemoveListeners()
        {
            RemoveListener(buyButton, Buy);
            RemoveListener(sellButton, Sell);
            RemoveListener(minusButton, DecreaseOrderQuantity);
            RemoveListener(plusButton, IncreaseOrderQuantity);
            RemoveListener(maxButton, SetMaxOrderQuantity);
        }

        private void RefreshButtonStates(bool canSell)
        {
            if (sellButton != null)
            {
                sellButton.interactable = canSell;
            }

            if (maxButton != null)
            {
                maxButton.interactable = canSell;
            }
        }

        private void CreateMissingUi()
        {
            if (rectTransform == null)
            {
                return;
            }

            EnsurePanelSize();

            float y = -12f;
            currentPriceText = currentPriceText != null ? currentPriceText : CreateText("Current Price", "Price -", y);
            y -= 30f;
            pnlPercentText = pnlPercentText != null ? pnlPercentText : CreateText("PnL Percent", "PnL -", y);
            y -= 24f;
            ownedSharesText = ownedSharesText != null ? ownedSharesText : CreateText("Owned Shares", "Shares -", y);
            y -= 22f;
            averageBuyPriceText = averageBuyPriceText != null ? averageBuyPriceText : CreateText("Average Buy Price", "Avg -", y);
            y -= 22f;
            stockValueText = stockValueText != null ? stockValueText : CreateText("Stock Value", "Value -", y);
            y -= 22f;
            unrealizedPnLText = unrealizedPnLText != null ? unrealizedPnLText : CreateText("Unrealized PnL", "PnL -", y);
            y -= 22f;
            fairPriceText = fairPriceText != null ? fairPriceText : CreateText("Fair Price", "Fair -", y);
            y -= 22f;
            cityValueText = cityValueText != null ? cityValueText : CreateText("City Value", "City -", y);
            y -= 22f;
            fairValueBiasText = fairValueBiasText != null ? fairValueBiasText : CreateText("Fair Value Bias", "Fair Bias -", y);
            y -= 22f;
            priceImpactText = priceImpactText != null ? priceImpactText : CreateText("Price Impact", "Price Impact -", y);
            y -= 22f;
            finalChangeText = finalChangeText != null ? finalChangeText : CreateText("Final Change", "Final Change -", y);
            y -= 36f;
            minusButton = minusButton != null ? minusButton : CreateButton("Order Minus Button", "-", new Vector2(-72f, y), new Vector2(34f, 28f));
            orderQuantityText = orderQuantityText != null ? orderQuantityText : CreateText("Order Quantity", "1", y + 10f);
            orderQuantityText.alignment = TextAlignmentOptions.Center;
            plusButton = plusButton != null ? plusButton : CreateButton("Order Plus Button", "+", new Vector2(72f, y), new Vector2(34f, 28f));
            y -= 34f;
            maxButton = maxButton != null ? maxButton : CreateButton("Order Max Button", "MAX", new Vector2(0f, y), new Vector2(80f, 28f));
            y -= 40f;
            orderTotalText = orderTotalText != null ? orderTotalText : CreateText("Order Total", "TOTAL -", y + 18f);
            orderTotalText.alignment = TextAlignmentOptions.Center;
            orderTotalText.richText = true;
            y -= 52f;
            buyButton = buyButton != null ? buyButton : CreateButton("Buy Button", "BUY", new Vector2(-45f, y), new Vector2(80f, 28f));
            sellButton = sellButton != null ? sellButton : CreateButton("Sell Button", "SELL", new Vector2(45f, y), new Vector2(80f, 28f));
        }

        private void EnsurePanelSize()
        {
            if (rectTransform.sizeDelta.x < 220f || rectTransform.sizeDelta.y < 390f)
            {
                rectTransform.sizeDelta = new Vector2(240f, 410f);
            }
        }

        private TMP_Text CreateText(string objectName, string text, float y)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(transform, worldPositionStays: false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, y);
            textRect.sizeDelta = new Vector2(-16f, 22f);

            TMP_Text label = textObject.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Left;
            label.color = neutralColor;
            return label;
        }

        private Button CreateButton(string objectName, string text, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, worldPositionStays: false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = anchoredPosition;
            buttonRect.sizeDelta = size;

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.16f, 0.18f, 0.22f, 0.92f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, worldPositionStays: false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = 13f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = neutralColor;

            return buttonObject.GetComponent<Button>();
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetTextColor(TMP_Text target, Color color)
        {
            if (target != null)
            {
                target.color = color;
            }
        }

        private static void SetRichText(TMP_Text target, bool enabled)
        {
            if (target != null)
            {
                target.richText = enabled;
            }
        }

        private static string FormatMoney(double value)
        {
            return "$" + value.ToString("#,0.00");
        }

        private static string FormatSignedMoney(double value)
        {
            return value >= 0d ? $"+{FormatMoney(value)}" : FormatMoney(value);
        }

        private static string FormatSignedPercent(double value)
        {
            return value >= 0d ? $"+{value:P1}" : value.ToString("P1");
        }

        private Color GetPnlColor(double pnl)
        {
            if (pnl > 0d)
            {
                return profitColor;
            }

            if (pnl < 0d)
            {
                return lossColor;
            }

            return neutralColor;
        }

        private long GetSafeOrderQuantity()
        {
            return orderQuantity < 1L ? 1L : orderQuantity;
        }

        private static long SafeAddLong(long current, long amount)
        {
            return long.MaxValue - current < amount ? long.MaxValue : current + amount;
        }
    }
}
