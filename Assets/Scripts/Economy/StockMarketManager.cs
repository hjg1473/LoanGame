using System;
using System.Collections.Generic;
using LoadGame.Audio;
using UnityEngine;

namespace LoadGame.Economy
{
    public sealed class StockMarketManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private CityValueManager cityValueManager;
        [SerializeField] private double startingStockPrice = 100d;
        [SerializeField] private double minStockPrice = 1d;
        [SerializeField] private double maxTickChangeRate = 0.005d;
        [SerializeField] private float stockTickInterval = 0.1f;
        [SerializeField] private float candleDuration = 5f;
        [SerializeField] private int maxCandles = 30;

        [Header("Economy Bias")]
        [SerializeField] private double productionBiasPerCashPerSecond = 0.000002d;
        [SerializeField] private double recentProductionBiasPerCashPerSecond = 0.000015d;
        [SerializeField] private double debtBiasPerDebt = 0.00000015d;
        [SerializeField] private double recentDebtBiasPerDebt = 0.000004d;
        [SerializeField] private double interestRateBiasMultiplier = 0.006d;
        [SerializeField] private double recentRepaymentBiasPerCash = 0.000003d;
        [SerializeField] private double economyPressureDecayPerSecond = 1.25d;
        [SerializeField] private double maxEconomyBias = 0.003d;

        [Header("Fair Value")]
        [SerializeField] private double valuationDivisor = 1d;
        [SerializeField] private double fairValueStrength = 0.0015d;
        [SerializeField] private double maxFairValueBias = 0.002d;

        [Header("Price Impact")]
        [SerializeField] private double priceImpactStrength = 0.02d;
        [SerializeField] private double maxPriceImpact = 0.01d;
        [SerializeField] private double priceImpactDecay = 0.85d;

        private readonly List<StockCandle> closedCandles = new List<StockCandle>(30);

        private double stockPrice;
        private StockCandle activeCandle;
        private long ownedShares;
        private double averageBuyPrice;
        private double lastCashPerSecond;
        private double recentProductionPressure;
        private double recentDebtPressure;
        private double recentRepaymentPressure;
        private double fairPrice;
        private double fairValueBias;
        private double transientPriceImpact;
        private double finalChangeRate;
        private float stockTickTimer;
        private float activeCandleElapsed;

        public double StockPrice => stockPrice;
        public double CurrentPrice => stockPrice;
        public double FairPrice => fairPrice;
        public double CityValue => cityValueManager == null ? 0d : cityValueManager.GetCityValue();
        public double FairValueBias => fairValueBias;
        public double PriceImpact => transientPriceImpact;
        public double FinalChangeRate => finalChangeRate;
        public IReadOnlyList<StockCandle> Candles => closedCandles;
        public IReadOnlyList<StockCandle> ClosedCandles => closedCandles;
        public StockCandle ActiveCandle => activeCandle;
        public long OwnedShares => ownedShares;
        public double AverageBuyPrice => averageBuyPrice;
        public double StockValue => ownedShares * stockPrice;
        public double UnrealizedPnL => ownedShares * (stockPrice - averageBuyPrice);

        public event Action<double> StockPriceChanged;
        public event Action StockPositionChanged;
        public event Action<IReadOnlyList<StockCandle>> CandlesChanged;
        public event Action<IReadOnlyList<StockCandle>, StockCandle> CandlesUpdated;

        private void Awake()
        {
            stockPrice = Math.Max(minStockPrice, startingStockPrice);
            StartNewActiveCandle(stockPrice);
        }

        private void OnEnable()
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (cityValueManager == null)
            {
                cityValueManager = FindFirstObjectByType<CityValueManager>();
            }

            if (economyManager != null)
            {
                lastCashPerSecond = economyManager.GetCashPerSecond();
                economyManager.ProductionChanged += OnProductionChanged;
                economyManager.DebtAdded += OnDebtAdded;
                economyManager.DebtRepaid += OnDebtRepaid;
            }
        }

        private void Start()
        {
            if (cityValueManager == null)
            {
                cityValueManager = FindFirstObjectByType<CityValueManager>();
            }

            fairPrice = CalculateFairPrice();
            fairValueBias = CalculateFairValueBias();
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.ProductionChanged -= OnProductionChanged;
                economyManager.DebtAdded -= OnDebtAdded;
                economyManager.DebtRepaid -= OnDebtRepaid;
            }
        }

        public IReadOnlyList<StockCandle> GetCandles()
        {
            return closedCandles;
        }

        public StockMarketSaveState CaptureSaveState()
        {
            return new StockMarketSaveState
            {
                stockPrice = stockPrice,
                ownedShares = ownedShares,
                averageBuyPrice = averageBuyPrice
            };
        }

        public void RestoreSaveState(StockMarketSaveState state)
        {
            if (state == null)
            {
                return;
            }

            stockPrice = Math.Max(minStockPrice, state.stockPrice);
            ownedShares = Math.Max(0L, state.ownedShares);
            averageBuyPrice = ownedShares > 0L ? Math.Max(0d, state.averageBuyPrice) : 0d;
            closedCandles.Clear();
            StartNewActiveCandle(stockPrice);
            fairPrice = CalculateFairPrice();
            fairValueBias = CalculateFairValueBias();
            StockPriceChanged?.Invoke(stockPrice);
            NotifyPositionChanged();
            CandlesUpdated?.Invoke(closedCandles, activeCandle);
        }

        public void SetCityValueManager(CityValueManager valueManager)
        {
            cityValueManager = valueManager;
            fairPrice = CalculateFairPrice();
            fairValueBias = CalculateFairValueBias();
            StockPriceChanged?.Invoke(stockPrice);
        }

        private void Update()
        {
            if (economyManager != null && economyManager.IsBankrupt())
            {
                return;
            }

            float safeTickInterval = GetSafeStockTickInterval();
            stockTickTimer += Time.deltaTime;

            while (stockTickTimer >= safeTickInterval)
            {
                stockTickTimer -= safeTickInterval;
                ProcessStockTick(safeTickInterval);
            }
        }

        private void ProcessStockTick(float tickInterval)
        {
            TickEconomyPressure(tickInterval);

            double randomChange = UnityEngine.Random.Range((float)-maxTickChangeRate, (float)maxTickChangeRate);
            double economyBias = CalculateEconomyBias();
            fairValueBias = CalculateFairValueBias();
            finalChangeRate = randomChange + economyBias + fairValueBias + transientPriceImpact;
            stockPrice = Math.Max(minStockPrice, stockPrice * (1d + finalChangeRate));
            transientPriceImpact *= GetSafePriceImpactDecay();
            StockPriceChanged?.Invoke(stockPrice);

            double high = Math.Max(activeCandle.High, stockPrice);
            double low = Math.Min(activeCandle.Low, stockPrice);
            activeCandle = new StockCandle(activeCandle.Open, high, low, stockPrice);
            activeCandleElapsed += tickInterval;
            CandlesUpdated?.Invoke(closedCandles, activeCandle);

            if (activeCandleElapsed >= GetSafeCandleDuration())
            {
                AddClosedCandle(activeCandle);
                StartNewActiveCandle(stockPrice);
                CandlesUpdated?.Invoke(closedCandles, activeCandle);
            }
        }

        public bool BuyOneShare()
        {
            return BuyShares(1L);
        }

        public bool BuyShares(long quantity)
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (economyManager == null || economyManager.IsBankrupt() || quantity <= 0L)
            {
                return false;
            }

            double totalCost = stockPrice * quantity;
            long cashCost = PriceToCashAmount(totalCost);
            economyManager.Pay(cashCost);
            AddTradePriceImpact(quantity, direction: 1d);

            double totalBuyCost = averageBuyPrice * ownedShares + totalCost;
            ownedShares = SafeAddLong(ownedShares, quantity);
            averageBuyPrice = totalBuyCost / ownedShares;
            SoundManager.Instance?.PlaySFX(SoundId.StockBuy);
            NotifyPositionChanged();
            return true;
        }

        public bool SellOneShare()
        {
            return SellShares(1L);
        }

        public bool SellShares(long quantity)
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (economyManager == null || economyManager.IsBankrupt() || ownedShares <= 0L || quantity <= 0L)
            {
                return false;
            }

            long sellQuantity = Math.Min(quantity, ownedShares);
            ownedShares -= sellQuantity;
            if (ownedShares == 0L)
            {
                averageBuyPrice = 0d;
            }

            economyManager.AddCash(PriceToCashAmount(stockPrice * sellQuantity));
            AddTradePriceImpact(sellQuantity, direction: -1d);
            SoundManager.Instance?.PlaySFX(SoundId.StockSell);
            NotifyPositionChanged();
            return true;
        }

        private void AddClosedCandle(StockCandle candle)
        {
            closedCandles.Add(candle);
            int safeMaxCandles = GetSafeMaxCandles();
            while (closedCandles.Count > safeMaxCandles)
            {
                closedCandles.RemoveAt(0);
            }

            CandlesChanged?.Invoke(closedCandles);
        }

        private void StartNewActiveCandle(double price)
        {
            activeCandle = new StockCandle(price, price, price, price);
            activeCandleElapsed = 0f;
        }

        private void NotifyPositionChanged()
        {
            StockPositionChanged?.Invoke();
        }

        private void OnProductionChanged(double cashPerSecond)
        {
            double increase = Math.Max(0d, cashPerSecond - lastCashPerSecond);
            recentProductionPressure = SafeAddDouble(recentProductionPressure, increase);
            lastCashPerSecond = cashPerSecond;
        }

        private void OnDebtAdded(long amount)
        {
            if (amount > 0L)
            {
                recentDebtPressure = SafeAddDouble(recentDebtPressure, amount);
            }
        }

        private void OnDebtRepaid(long amount)
        {
            if (amount > 0L)
            {
                recentRepaymentPressure = SafeAddDouble(recentRepaymentPressure, amount);
            }
        }

        private void TickEconomyPressure(float elapsedSeconds)
        {
            double decay = Math.Max(0d, 1d - GetSafeEconomyPressureDecayPerSecond() * elapsedSeconds);
            recentProductionPressure *= decay;
            recentDebtPressure *= decay;
            recentRepaymentPressure *= decay;
        }

        private double CalculateEconomyBias()
        {
            if (economyManager == null)
            {
                return 0d;
            }

            double productionBias = economyManager.GetCashPerSecond() * productionBiasPerCashPerSecond;
            double recentProductionBias = recentProductionPressure * recentProductionBiasPerCashPerSecond;
            double debtBias = economyManager.GetDebt() * debtBiasPerDebt;
            double recentDebtBias = recentDebtPressure * recentDebtBiasPerDebt;
            double interestBias = Math.Max(0d, economyManager.GetCurrentInterestRate() - economyManager.GetBaseInterestRate()) * interestRateBiasMultiplier;
            double repaymentBias = recentRepaymentPressure * recentRepaymentBiasPerCash;
            double economyBias = productionBias + recentProductionBias + repaymentBias - debtBias - recentDebtBias - interestBias;

            return Clamp(economyBias, -GetSafeMaxEconomyBias(), GetSafeMaxEconomyBias());
        }

        private double CalculateFairValueBias()
        {
            fairPrice = CalculateFairPrice();
            if (fairPrice <= 0d)
            {
                return 0d;
            }

            double fairValueDifference = (fairPrice - stockPrice) / fairPrice;
            double bias = fairValueDifference * fairValueStrength;
            return Clamp(bias, -GetSafeMaxFairValueBias(), GetSafeMaxFairValueBias());
        }

        private double CalculateFairPrice()
        {
            if (cityValueManager == null)
            {
                return 0d;
            }

            return Math.Max(minStockPrice, cityValueManager.GetCityValue() / GetSafeValuationDivisor());
        }

        private void AddTradePriceImpact(long tradedShares, double direction)
        {
            if (tradedShares <= 0L || direction == 0d)
            {
                return;
            }

            double cityValue = Math.Max(1d, CityValue);
            double tradeValue = stockPrice * tradedShares;
            double tradeRatio = tradeValue / cityValue;
            double impact = tradeRatio * priceImpactStrength * Math.Sign(direction);
            transientPriceImpact = Clamp(transientPriceImpact + impact, -GetSafeMaxPriceImpact(), GetSafeMaxPriceImpact());
        }

        private static long PriceToCashAmount(double price)
        {
            if (price <= 0d)
            {
                return 1L;
            }

            return (long)Math.Max(1d, Math.Round(price, MidpointRounding.AwayFromZero));
        }

        private static double SafeAddDouble(double current, double amount)
        {
            return double.MaxValue - current < amount ? double.MaxValue : current + amount;
        }

        private static long SafeAddLong(long current, long amount)
        {
            return long.MaxValue - current < amount ? long.MaxValue : current + amount;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        private float GetSafeStockTickInterval()
        {
            return stockTickInterval > 0f ? stockTickInterval : 0.1f;
        }

        private float GetSafeCandleDuration()
        {
            return candleDuration > 0f ? candleDuration : 5f;
        }

        private int GetSafeMaxCandles()
        {
            return maxCandles > 0 ? maxCandles : 30;
        }

        private double GetSafeEconomyPressureDecayPerSecond()
        {
            return economyPressureDecayPerSecond > 0d ? economyPressureDecayPerSecond : 1.25d;
        }

        private double GetSafeMaxEconomyBias()
        {
            return maxEconomyBias > 0d ? maxEconomyBias : 0.003d;
        }

        private double GetSafeValuationDivisor()
        {
            return valuationDivisor > 0d ? valuationDivisor : 1d;
        }

        private double GetSafeMaxFairValueBias()
        {
            return maxFairValueBias > 0d ? maxFairValueBias : 0.002d;
        }

        private double GetSafeMaxPriceImpact()
        {
            return maxPriceImpact > 0d ? maxPriceImpact : 0.01d;
        }

        private double GetSafePriceImpactDecay()
        {
            return Clamp(priceImpactDecay, 0d, 1d);
        }
    }
}
