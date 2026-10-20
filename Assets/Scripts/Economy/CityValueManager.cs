using System;
using LoadGame.Map;
using UnityEngine;

namespace LoadGame.Economy
{
    public sealed class CityValueManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;

        [Header("Value Formula")]
        [SerializeField] private long landValue = 100L;
        [SerializeField] private double productionMultiplier = 120d;

        private GridMapData mapData;
        private double cityValue;
        private double landValueTotal;
        private double buildingValueTotal;
        private double productionValue;
        private double completedProjectValueBonus;
        private bool initialized;

        public double CityValue => cityValue;
        public double LandValueTotal => landValueTotal;
        public double BuildingValueTotal => buildingValueTotal;
        public double ProductionValue => productionValue;
        public double CompletedProjectValueBonus => completedProjectValueBonus;

        public event Action<double> CityValueChanged;

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

        public void Initialize(GridMapData data, ChunkedGridMapRenderer renderer, EconomyManager economy)
        {
            Unsubscribe();

            mapData = data;
            mapRenderer = renderer;
            economyManager = economy;
            initialized = mapData != null && mapRenderer != null && economyManager != null;

            if (!initialized)
            {
                return;
            }

            economyManager.ProductionChanged += OnProductionChanged;
            mapData.CellChanged += OnCellChanged;
            RecalculateCityValue();
        }

        public double GetCityValue()
        {
            return cityValue;
        }

        public void RecalculateCityValue()
        {
            if (mapData == null || mapRenderer == null || economyManager == null)
            {
                return;
            }

            landValueTotal = CalculateLandValue();
            buildingValueTotal = CalculateBuildingValue();
            productionValue = economyManager.GetCashPerSecond() * productionMultiplier;

            double calculatedValue = landValueTotal + buildingValueTotal + productionValue + completedProjectValueBonus;

            cityValue = Math.Max(0d, calculatedValue);
            CityValueChanged?.Invoke(cityValue);
        }

        private void TryInitializeFromScene()
        {
            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (mapManager == null)
            {
                mapManager = FindFirstObjectByType<MapManager>();
            }

            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<ChunkedGridMapRenderer>();
            }

            GridMapData data = mapManager == null ? null : mapManager.MapData;
            Initialize(data, mapRenderer, economyManager);
        }

        private double CalculateLandValue()
        {
            double total = 0d;
            foreach (GridCellData cell in mapData.Cells)
            {
                if (cell != null && cell.owned && cell.CellType == GridCellType.Empty)
                {
                    total += landValue;
                }
            }

            return total;
        }

        private double CalculateBuildingValue()
        {
            double total = 0d;
            foreach (BuildingInstance instance in mapData.BuildingInstances)
            {
                if (instance == null || instance.Definition == null)
                {
                    continue;
                }

                total += instance.GetBuildingValue();
            }

            return total;
        }

        private void OnProductionChanged(double cashPerSecond)
        {
            RecalculateCityValue();
        }

        private void OnCellChanged(GridCellData cell)
        {
            RecalculateCityValue();
        }

        public void AddCompletedProjectValueBonus(double amount)
        {
            if (amount <= 0d)
            {
                return;
            }

            completedProjectValueBonus += amount;
            RecalculateCityValue();
        }

        public void SetCompletedProjectValueBonus(double amount)
        {
            completedProjectValueBonus = Math.Max(0d, amount);
            RecalculateCityValue();
        }

        private void Unsubscribe()
        {
            if (economyManager != null)
            {
                economyManager.ProductionChanged -= OnProductionChanged;
            }

            if (mapData != null)
            {
                mapData.CellChanged -= OnCellChanged;
            }

            initialized = false;
        }
    }
}
