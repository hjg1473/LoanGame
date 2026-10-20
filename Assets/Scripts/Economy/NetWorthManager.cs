using System;
using UnityEngine;

namespace LoadGame.Economy
{
    public sealed class NetWorthManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private CityValueManager cityValueManager;
        [SerializeField] private StockMarketManager stockMarketManager;

        private double netWorth;
        private bool initialized;

        public double NetWorth => netWorth;
        public double StockHoldingsValue => stockMarketManager == null ? 0d : stockMarketManager.StockValue;

        public event Action<double> NetWorthChanged;

        private void Start()
        {
            if (!initialized)
            {
                TryInitializeFromScene();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Initialize(EconomyManager economy, CityValueManager cityValue, StockMarketManager stockMarket)
        {
            Unsubscribe();

            economyManager = economy;
            cityValueManager = cityValue;
            stockMarketManager = stockMarket;
            initialized = economyManager != null && cityValueManager != null;

            if (!initialized)
            {
                return;
            }

            economyManager.CashChanged += OnCashChanged;
            economyManager.DebtChanged += OnDebtChanged;
            cityValueManager.CityValueChanged += OnCityValueChanged;

            if (stockMarketManager != null)
            {
                stockMarketManager.StockPriceChanged += OnStockPriceChanged;
                stockMarketManager.StockPositionChanged += OnStockPositionChanged;
            }

            RecalculateNetWorth();
        }

        public double GetNetWorth()
        {
            return netWorth;
        }

        public void RecalculateNetWorth()
        {
            if (economyManager == null || cityValueManager == null)
            {
                return;
            }

            double calculatedNetWorth = economyManager.GetCash();
            calculatedNetWorth += cityValueManager.LandValueTotal;
            calculatedNetWorth += cityValueManager.BuildingValueTotal;
            calculatedNetWorth += StockHoldingsValue;
            calculatedNetWorth -= economyManager.GetDebt();

            netWorth = calculatedNetWorth;
            NetWorthChanged?.Invoke(netWorth);
        }

        private void TryInitializeFromScene()
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (cityValueManager == null)
            {
                cityValueManager = FindFirstObjectByType<CityValueManager>();
            }

            if (stockMarketManager == null)
            {
                stockMarketManager = FindFirstObjectByType<StockMarketManager>();
            }

            Initialize(economyManager, cityValueManager, stockMarketManager);
        }

        private void OnCashChanged(long cash)
        {
            RecalculateNetWorth();
        }

        private void OnDebtChanged(long debt)
        {
            RecalculateNetWorth();
        }

        private void OnCityValueChanged(double cityValue)
        {
            RecalculateNetWorth();
        }

        private void OnStockPriceChanged(double stockPrice)
        {
            RecalculateNetWorth();
        }

        private void OnStockPositionChanged()
        {
            RecalculateNetWorth();
        }

        private void Unsubscribe()
        {
            if (economyManager != null)
            {
                economyManager.CashChanged -= OnCashChanged;
                economyManager.DebtChanged -= OnDebtChanged;
            }

            if (cityValueManager != null)
            {
                cityValueManager.CityValueChanged -= OnCityValueChanged;
            }

            if (stockMarketManager != null)
            {
                stockMarketManager.StockPriceChanged -= OnStockPriceChanged;
                stockMarketManager.StockPositionChanged -= OnStockPositionChanged;
            }

            initialized = false;
        }
    }
}
