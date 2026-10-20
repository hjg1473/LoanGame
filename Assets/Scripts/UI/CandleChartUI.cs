using System.Collections.Generic;
using LoadGame.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoadGame.UI
{
    public sealed class CandleChartUI : MonoBehaviour
    {
        [SerializeField] private StockMarketManager stockMarketManager;
        [SerializeField] private RectTransform chartPanel;
        [SerializeField] private CandleView candlePrefab;
        [SerializeField] private int visibleCandleCount = 30;
        [SerializeField] private float horizontalPadding = 12f;
        [SerializeField] private float verticalPadding = 10f;
        [SerializeField] private float candleGap = 3f;
        [SerializeField] private float priceLabelWidth = 56f;
        [SerializeField] private int gridRowCount = 5;
        [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color gridLabelColor = new Color(1f, 1f, 1f, 0.72f);
        [SerializeField] private Color averageLineColor = new Color(1f, 0.9f, 0.35f, 0.78f);
        [SerializeField] private int averageLineSegmentCount = 18;
        [SerializeField] private float averageLineThickness = 1.5f;
        [SerializeField] private float averageLineGap = 4f;
        [SerializeField] private Color risingColor = new Color(0.2f, 0.8f, 0.45f, 1f);
        [SerializeField] private Color fallingColor = new Color(0.95f, 0.25f, 0.25f, 1f);
        [SerializeField] private bool generateTestCandlesOnStart;
        [SerializeField] private double testStartPrice = 100d;

        private readonly List<CandleView> candleViews = new List<CandleView>(30);
        private readonly List<Image> gridLines = new List<Image>(5);
        private readonly List<TMP_Text> gridLabels = new List<TMP_Text>(5);
        private readonly List<Image> averageLineSegments = new List<Image>(18);
        private readonly List<StockCandle> testCandles = new List<StockCandle>(30);
        private RectTransform gridRoot;
        private RectTransform averageLineRoot;
        private TMP_Text averagePriceLabel;
        private StockCandle testActiveCandle;

        private void Awake()
        {
            if (chartPanel == null)
            {
                chartPanel = transform as RectTransform;
            }

            EnsureCandlePool();
            EnsureGridPool();
            EnsureAverageLinePool();
        }

        private void Start()
        {
            if (stockMarketManager == null)
            {
                stockMarketManager = FindFirstObjectByType<StockMarketManager>();
            }

            if (stockMarketManager != null)
            {
                stockMarketManager.CandlesUpdated += RenderCandles;
            }

            if (stockMarketManager != null)
            {
                RenderCandles(stockMarketManager.ClosedCandles, stockMarketManager.ActiveCandle);
                return;
            }

            if (generateTestCandlesOnStart)
            {
                GenerateTestCandles();
                RenderCandles(testCandles, testActiveCandle);
            }
            else
            {
                HideAllCandles();
            }
        }

        private void OnDisable()
        {
            if (stockMarketManager != null)
            {
                stockMarketManager.CandlesUpdated -= RenderCandles;
            }
        }

        public void RenderCandles(IReadOnlyList<StockCandle> candles)
        {
            if (candles == null || candles.Count == 0)
            {
                HideAllCandles();
                HideGrid();
                HideAverageLine();
                return;
            }

            RenderClosedCandlesOnly(candles);
        }

        public void RenderCandles(IReadOnlyList<StockCandle> closedCandles, StockCandle activeCandle)
        {
            if (chartPanel == null)
            {
                HideAllCandles();
                HideGrid();
                HideAverageLine();
                return;
            }

            EnsureCandlePool();
            EnsureGridPool();
            EnsureAverageLinePool();

            int safeVisibleCount = GetSafeVisibleCandleCount();
            int closedCount = closedCandles == null ? 0 : closedCandles.Count;
            int visibleCount = Mathf.Min(safeVisibleCount, closedCount + 1);
            int closedVisibleCount = Mathf.Min(closedCount, Mathf.Max(0, visibleCount - 1));
            int closedStartIndex = closedCount - closedVisibleCount;

            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;
            for (int i = 0; i < closedVisibleCount; i++)
            {
                StockCandle candle = closedCandles[closedStartIndex + i];
                minPrice = System.Math.Min(minPrice, candle.Low);
                maxPrice = System.Math.Max(maxPrice, candle.High);
            }

            minPrice = System.Math.Min(minPrice, activeCandle.Low);
            maxPrice = System.Math.Max(maxPrice, activeCandle.High);

            double priceRange = System.Math.Max(0.0001d, maxPrice - minPrice);
            float panelWidth = chartPanel.rect.width;
            float panelHeight = chartPanel.rect.height;
            float safeLabelWidth = Mathf.Max(0f, priceLabelWidth);
            float drawableWidth = Mathf.Max(1f, panelWidth - horizontalPadding * 2f - safeLabelWidth);
            float drawableHeight = Mathf.Max(1f, panelHeight - verticalPadding * 2f);
            float candleWidth = Mathf.Max(2f, (drawableWidth - candleGap * (safeVisibleCount - 1)) / safeVisibleCount);
            float step = candleWidth + candleGap;
            float leftX = -panelWidth * 0.5f + horizontalPadding + candleWidth * 0.5f;
            float lineLeftX = -panelWidth * 0.5f + horizontalPadding;
            float lineRightX = lineLeftX + drawableWidth;

            RenderGrid(minPrice, maxPrice, priceRange, drawableHeight, panelHeight, lineLeftX, lineRightX);

            for (int i = 0; i < candleViews.Count; i++)
            {
                if (i >= visibleCount)
                {
                    candleViews[i].SetVisible(false);
                    continue;
                }

                StockCandle candle = i < closedVisibleCount ? closedCandles[closedStartIndex + i] : activeCandle;
                RenderCandleAt(candleViews[i], candle, leftX + i * step, candleWidth, minPrice, priceRange, drawableHeight, panelHeight);
            }

            RenderAverageLine(minPrice, priceRange, drawableHeight, panelHeight, lineLeftX, lineRightX);
        }

        public void RenderCandles()
        {
            if (stockMarketManager != null)
            {
                RenderCandles(stockMarketManager.ClosedCandles, stockMarketManager.ActiveCandle);
            }
        }

        private void RenderClosedCandlesOnly(IReadOnlyList<StockCandle> candles)
        {
            EnsureCandlePool();

            int safeVisibleCount = GetSafeVisibleCandleCount();
            int visibleCount = Mathf.Min(safeVisibleCount, candles.Count);
            int startIndex = candles.Count - visibleCount;

            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;
            for (int i = 0; i < visibleCount; i++)
            {
                StockCandle candle = candles[startIndex + i];
                minPrice = System.Math.Min(minPrice, candle.Low);
                maxPrice = System.Math.Max(maxPrice, candle.High);
            }

            double priceRange = System.Math.Max(0.0001d, maxPrice - minPrice);
            float panelWidth = chartPanel.rect.width;
            float panelHeight = chartPanel.rect.height;
            float safeLabelWidth = Mathf.Max(0f, priceLabelWidth);
            float drawableWidth = Mathf.Max(1f, panelWidth - horizontalPadding * 2f - safeLabelWidth);
            float drawableHeight = Mathf.Max(1f, panelHeight - verticalPadding * 2f);
            float candleWidth = Mathf.Max(2f, (drawableWidth - candleGap * (safeVisibleCount - 1)) / safeVisibleCount);
            float step = candleWidth + candleGap;
            float leftX = -panelWidth * 0.5f + horizontalPadding + candleWidth * 0.5f;
            float lineLeftX = -panelWidth * 0.5f + horizontalPadding;
            float lineRightX = lineLeftX + drawableWidth;

            RenderGrid(minPrice, maxPrice, priceRange, drawableHeight, panelHeight, lineLeftX, lineRightX);

            for (int i = 0; i < candleViews.Count; i++)
            {
                if (i >= visibleCount)
                {
                    candleViews[i].SetVisible(false);
                    continue;
                }

                RenderCandleAt(candleViews[i], candles[startIndex + i], leftX + i * step, candleWidth, minPrice, priceRange, drawableHeight, panelHeight);
            }

            HideAverageLine();
        }

        private void RenderCandleAt(CandleView view, StockCandle candle, float x, float candleWidth, double minPrice, double priceRange, float drawableHeight, float panelHeight)
        {
            float openY = PriceToY(candle.Open, minPrice, priceRange, drawableHeight, verticalPadding, panelHeight);
            float highY = PriceToY(candle.High, minPrice, priceRange, drawableHeight, verticalPadding, panelHeight);
            float lowY = PriceToY(candle.Low, minPrice, priceRange, drawableHeight, verticalPadding, panelHeight);
            float closeY = PriceToY(candle.Close, minPrice, priceRange, drawableHeight, verticalPadding, panelHeight);
            float bodyCenterY = (openY + closeY) * 0.5f;
            float bodyHeight = Mathf.Abs(closeY - openY);
            float wickCenterY = (highY + lowY) * 0.5f;
            float wickHeight = Mathf.Abs(highY - lowY);
            Color candleColor = candle.Close >= candle.Open ? risingColor : fallingColor;

            view.SetVisible(true);
            view.Render(new Vector2(x, 0f), candleWidth, bodyCenterY, bodyHeight, wickCenterY, wickHeight, candleColor);
        }

        private void EnsureCandlePool()
        {
            int targetCount = GetSafeVisibleCandleCount();
            while (candleViews.Count < targetCount)
            {
                candleViews.Add(CreateCandleView(candleViews.Count));
            }
        }

        private void EnsureGridPool()
        {
            if (chartPanel == null)
            {
                return;
            }

            if (gridRoot == null)
            {
                GameObject rootObject = new GameObject("Chart Grid", typeof(RectTransform));
                rootObject.transform.SetParent(chartPanel, worldPositionStays: false);
                gridRoot = rootObject.GetComponent<RectTransform>();
                StretchToParent(gridRoot);
                gridRoot.SetAsFirstSibling();
            }

            int targetCount = GetSafeGridRowCount();
            while (gridLines.Count < targetCount)
            {
                gridLines.Add(CreateLine(gridRoot, $"Grid Line {gridLines.Count:00}", gridLineColor));
            }

            while (gridLabels.Count < targetCount)
            {
                gridLabels.Add(CreateGridLabel(gridRoot, $"Grid Label {gridLabels.Count:00}"));
            }
        }

        private void EnsureAverageLinePool()
        {
            if (chartPanel == null)
            {
                return;
            }

            if (averageLineRoot == null)
            {
                GameObject rootObject = new GameObject("Average Buy Line", typeof(RectTransform));
                rootObject.transform.SetParent(chartPanel, worldPositionStays: false);
                averageLineRoot = rootObject.GetComponent<RectTransform>();
                StretchToParent(averageLineRoot);
            }

            int targetCount = GetSafeAverageLineSegmentCount();
            while (averageLineSegments.Count < targetCount)
            {
                averageLineSegments.Add(CreateLine(averageLineRoot, $"Average Segment {averageLineSegments.Count:00}", averageLineColor));
            }

            if (averagePriceLabel == null)
            {
                averagePriceLabel = CreateGridLabel(averageLineRoot, "Average Price Label");
                averagePriceLabel.color = averageLineColor;
                averagePriceLabel.alignment = TextAlignmentOptions.Left;
            }

            averageLineRoot.SetAsLastSibling();
        }

        private void RenderGrid(double minPrice, double maxPrice, double priceRange, float drawableHeight, float panelHeight, float lineLeftX, float lineRightX)
        {
            EnsureGridPool();

            int rowCount = GetSafeGridRowCount();
            float lineWidth = Mathf.Max(1f, lineRightX - lineLeftX);
            float centerX = (lineLeftX + lineRightX) * 0.5f;
            float labelX = lineRightX + Mathf.Max(4f, priceLabelWidth * 0.08f);

            for (int i = 0; i < gridLines.Count; i++)
            {
                bool visible = i < rowCount;
                gridLines[i].gameObject.SetActive(visible);
                gridLabels[i].gameObject.SetActive(visible);

                if (!visible)
                {
                    continue;
                }

                float t = rowCount == 1 ? 0f : i / (float)(rowCount - 1);
                double price = minPrice + priceRange * t;
                float y = PriceToY(price, minPrice, priceRange, drawableHeight, verticalPadding, panelHeight);

                RectTransform lineRect = gridLines[i].rectTransform;
                lineRect.anchoredPosition = new Vector2(centerX, y);
                lineRect.sizeDelta = new Vector2(lineWidth, 1f);
                gridLines[i].color = gridLineColor;

                RectTransform labelRect = gridLabels[i].rectTransform;
                labelRect.anchoredPosition = new Vector2(labelX, y);
                labelRect.sizeDelta = new Vector2(Mathf.Max(24f, priceLabelWidth), 18f);
                gridLabels[i].text = FormatMoney(price);
                gridLabels[i].color = gridLabelColor;
            }
        }

        private void RenderAverageLine(double minPrice, double priceRange, float drawableHeight, float panelHeight, float lineLeftX, float lineRightX)
        {
            if (stockMarketManager == null || stockMarketManager.OwnedShares <= 0L)
            {
                HideAverageLine();
                return;
            }

            double averagePrice = stockMarketManager.AverageBuyPrice;
            if (averagePrice < minPrice || averagePrice > minPrice + priceRange)
            {
                HideAverageLine();
                return;
            }

            EnsureAverageLinePool();
            averageLineRoot.gameObject.SetActive(true);

            int segmentCount = GetSafeAverageLineSegmentCount();
            float y = PriceToY(averagePrice, minPrice, priceRange, drawableHeight, verticalPadding, panelHeight);
            float lineWidth = Mathf.Max(1f, lineRightX - lineLeftX);
            float segmentWidth = Mathf.Max(2f, (lineWidth - averageLineGap * (segmentCount - 1)) / segmentCount);

            for (int i = 0; i < averageLineSegments.Count; i++)
            {
                bool visible = i < segmentCount;
                averageLineSegments[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                RectTransform rect = averageLineSegments[i].rectTransform;
                rect.anchoredPosition = new Vector2(lineLeftX + segmentWidth * 0.5f + i * (segmentWidth + averageLineGap), y);
                rect.sizeDelta = new Vector2(segmentWidth, Mathf.Max(1f, averageLineThickness));
                averageLineSegments[i].color = averageLineColor;
            }

            if (averagePriceLabel != null)
            {
                averagePriceLabel.gameObject.SetActive(true);
                averagePriceLabel.text = $"AVG {FormatMoney(averagePrice)}";
                averagePriceLabel.color = averageLineColor;
                RectTransform labelRect = averagePriceLabel.rectTransform;
                labelRect.anchoredPosition = new Vector2(lineRightX + Mathf.Max(4f, priceLabelWidth * 0.08f), y + 10f);
                labelRect.sizeDelta = new Vector2(Mathf.Max(42f, priceLabelWidth), 18f);
            }
        }

        private CandleView CreateCandleView(int index)
        {
            if (candlePrefab != null)
            {
                CandleView instance = Instantiate(candlePrefab, chartPanel);
                instance.name = $"Candle {index:00}";
                return instance;
            }

            GameObject rootObject = new GameObject($"Candle {index:00}", typeof(RectTransform));
            rootObject.transform.SetParent(chartPanel, worldPositionStays: false);
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            RectTransform wickRect = CreateImageChild(rootObject.transform, "Wick", out Image wickImage);
            RectTransform bodyRect = CreateImageChild(rootObject.transform, "Body", out Image bodyImage);

            CandleView view = rootObject.AddComponent<CandleView>();
            view.Initialize(rootRect, bodyRect, wickRect, bodyImage, wickImage);
            return view;
        }

        private static RectTransform CreateImageChild(Transform parent, string name, out Image image)
        {
            GameObject childObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            childObject.transform.SetParent(parent, worldPositionStays: false);
            RectTransform rectTransform = childObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image = childObject.GetComponent<Image>();
            image.raycastTarget = false;
            return rectTransform;
        }

        private static Image CreateLine(Transform parent, string name, Color color)
        {
            GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(parent, worldPositionStays: false);
            RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Image image = lineObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text CreateGridLabel(Transform parent, string name)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, worldPositionStays: false);
            RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);

            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.fontSize = 10f;
            label.alignment = TextAlignmentOptions.Left;
            label.color = gridLabelColor;
            label.raycastTarget = false;
            return label;
        }

        private void GenerateTestCandles()
        {
            testCandles.Clear();
            double price = testStartPrice;
            int closedTestCount = Mathf.Max(0, GetSafeVisibleCandleCount() - 1);
            for (int i = 0; i < closedTestCount; i++)
            {
                double open = price;
                double close = System.Math.Max(1d, open * (1d + Random.Range(-0.04f, 0.04f)));
                double high = System.Math.Max(open, close) * (1d + Random.Range(0.005f, 0.035f));
                double low = System.Math.Max(1d, System.Math.Min(open, close) * (1d - Random.Range(0.005f, 0.035f)));
                testCandles.Add(new StockCandle(open, high, low, close));
                price = close;
            }

            testActiveCandle = new StockCandle(price, price, price, price);
        }

        private void HideAllCandles()
        {
            for (int i = 0; i < candleViews.Count; i++)
            {
                candleViews[i].SetVisible(false);
            }
        }

        private void HideGrid()
        {
            for (int i = 0; i < gridLines.Count; i++)
            {
                gridLines[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < gridLabels.Count; i++)
            {
                gridLabels[i].gameObject.SetActive(false);
            }
        }

        private void HideAverageLine()
        {
            if (averageLineRoot != null)
            {
                averageLineRoot.gameObject.SetActive(false);
            }
        }

        private int GetSafeVisibleCandleCount()
        {
            return visibleCandleCount > 0 ? visibleCandleCount : 30;
        }

        private int GetSafeGridRowCount()
        {
            return gridRowCount > 0 ? gridRowCount : 5;
        }

        private int GetSafeAverageLineSegmentCount()
        {
            return averageLineSegmentCount > 0 ? averageLineSegmentCount : 18;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static float PriceToY(double price, double minPrice, double priceRange, float drawableHeight, float verticalPadding, float panelHeight)
        {
            double normalized = (price - minPrice) / priceRange;
            return -panelHeight * 0.5f + verticalPadding + (float)(normalized * drawableHeight);
        }

        private static string FormatMoney(double value)
        {
            return "$" + value.ToString("#,0.##");
        }
    }
}
