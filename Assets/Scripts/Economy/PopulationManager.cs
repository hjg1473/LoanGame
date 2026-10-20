using System;
using LoadGame.Map;
using UnityEngine;
using LoadGame.Balance;

namespace LoadGame.Economy
{
    public sealed class PopulationManager : MonoBehaviour
    {
        [Header("Population Tick")]
        [SerializeField, Min(0.1f)] private float populationTickInterval = 0.5f;
        [SerializeField, Range(0f, 100f)] private float minHappiness = 40f;

        [Header("Move-in Rates (Population / Second)")]
        [SerializeField, Range(0f, 100f)] private float stoppedHappinessThreshold = 40f;
        [SerializeField, Range(0f, 100f)] private float highHappinessThreshold = 75f;
        [SerializeField, Min(0f)] private double lowHappinessMoveInRate = 0.2d;
        [SerializeField, Min(0f)] private double normalMoveInRate = 1d;
        [SerializeField, Min(0f)] private double highHappinessMoveInRate = 2d;

        private GridMapData mapData;
        private EconomyManager economyManager;
        private float tickTimer;
        private double population;
        private long populationCapacity;
        private long totalRequiredWorkers;
        private long totalLeisureCapacity;
        private double employedPopulation;
        private double employmentEfficiency;
        private double leisureCoverage = 1d;
        private float happiness = 100f;
        private double workplaceProduction;
        private double legacyProduction;
        private double totalMaintenancePerSecond;

        public static PopulationManager Instance { get; private set; }
        public event Action<double, long> PopulationChanged;
        public event Action<double, long, double> EmploymentChanged;
        public event Action<float> HappinessChanged;
        public event Action StatsChanged;

        public double Population => population;
        public long PopulationCapacity => populationCapacity;
        public long TotalRequiredWorkers => totalRequiredWorkers;
        public long TotalLeisureCapacity => totalLeisureCapacity;
        public double EmployedPopulation => employedPopulation;
        public double EmploymentEfficiency => employmentEfficiency;
        public double LeisureCoverage => leisureCoverage;
        public float Happiness => happiness;
        public double TotalMaintenancePerSecond => totalMaintenancePerSecond;

        public void RestorePopulation(double value)
        {
            population = Math.Max(0d, value);
            RecalculateBuildingAggregates();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple PopulationManager instances found. Disabling duplicate.", this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        public void Initialize(GridMapData data, EconomyManager economy)
        {
            UnbindMap();
            mapData = data;
            economyManager = economy;
            if (mapData != null)
            {
                mapData.BuildingPlaced += OnBuildingChanged;
                mapData.BuildingRemoved += OnBuildingChanged;
            }

            RecalculateBuildingAggregates();
        }

        private void Update()
        {
            if (mapData == null || economyManager == null || economyManager.IsBankrupt())
            {
                return;
            }

            tickTimer += Time.deltaTime;
            float interval = Mathf.Max(0.1f, populationTickInterval);
            while (tickTimer >= interval)
            {
                tickTimer -= interval;
                ProcessPopulationTick(interval);
            }
        }

        private void OnDestroy()
        {
            UnbindMap();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public double GetProductionMultiplier(BuildingDefinition definition)
        {
            return definition != null && definition.buildingRole == BuildingRole.Workplace
                ? employmentEfficiency
                : 1d;
        }

        private void ProcessPopulationTick(float elapsedSeconds)
        {
            if (population >= populationCapacity)
            {
                return;
            }

            double moveInRate = GetMoveInRate(happiness);
            if (moveInRate <= 0d)
            {
                return;
            }

            population = Math.Min(populationCapacity, population + moveInRate * elapsedSeconds);
            RecalculateDerivedStats();
        }

        private double GetMoveInRate(float currentHappiness)
        {
            return PopulationRules.MoveInRate(currentHappiness, stoppedHappinessThreshold, highHappinessThreshold, lowHappinessMoveInRate, normalMoveInRate, highHappinessMoveInRate);
        }

        private void OnBuildingChanged(BuildingInstance instance)
        {
            RecalculateBuildingAggregates();
        }

        private void RecalculateBuildingAggregates()
        {
            populationCapacity = 0L;
            totalRequiredWorkers = 0L;
            totalLeisureCapacity = 0L;
            workplaceProduction = 0d;
            legacyProduction = 0d;
            totalMaintenancePerSecond = 0d;

            if (mapData != null)
            {
                foreach (BuildingInstance instance in mapData.BuildingInstances)
                {
                    BuildingDefinition definition = instance.Definition;
                    if (definition == null)
                    {
                        continue;
                    }

                    totalMaintenancePerSecond += Math.Max(0d, definition.maintenancePerSecond);

                    switch (definition.buildingRole)
                    {
                        case BuildingRole.Residential:
                            populationCapacity = SafeAdd(populationCapacity, definition.populationCapacity);
                            break;
                        case BuildingRole.Workplace:
                            totalRequiredWorkers = SafeAdd(totalRequiredWorkers, definition.requiredWorkers);
                            workplaceProduction += instance.GetProductionPerSecond();
                            break;
                        case BuildingRole.Leisure:
                            totalLeisureCapacity = SafeAdd(totalLeisureCapacity, definition.leisureCapacity);
                            break;
                        default:
                            legacyProduction += instance.GetProductionPerSecond();
                            break;
                    }
                }
            }

            population = Math.Min(population, populationCapacity);
            RecalculateDerivedStats();
        }

        private void RecalculateDerivedStats()
        {
            PopulationDerivedState derived = PopulationRules.Derive(population, totalRequiredWorkers, totalLeisureCapacity, minHappiness);
            employedPopulation = derived.EmployedPopulation;
            employmentEfficiency = derived.EmploymentEfficiency;
            leisureCoverage = derived.LeisureCoverage;
            happiness = derived.Happiness;

            economyManager?.SetOperatingRates(
                legacyProduction + workplaceProduction * employmentEfficiency,
                totalMaintenancePerSecond);
            PopulationChanged?.Invoke(population, populationCapacity);
            EmploymentChanged?.Invoke(employedPopulation, totalRequiredWorkers, employmentEfficiency);
            HappinessChanged?.Invoke(happiness);
            StatsChanged?.Invoke();
        }

        private void UnbindMap()
        {
            if (mapData != null)
            {
                mapData.BuildingPlaced -= OnBuildingChanged;
                mapData.BuildingRemoved -= OnBuildingChanged;
            }

            mapData = null;
        }

        private static long SafeAdd(long current, int amount)
        {
            if (amount <= 0 || current >= long.MaxValue - amount)
            {
                return amount <= 0 ? current : long.MaxValue;
            }

            return current + amount;
        }

        private static double Lerp(double a, double b, float t)
        {
            return a + (b - a) * Mathf.Clamp01(t);
        }
    }
}
