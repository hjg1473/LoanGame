using System;
using System.Collections.Generic;
using LoadGame.Map;
using LoadGame.Economy;
using LoadGame.Upgrades;
using UnityEngine;

namespace LoadGame.UI
{
    public sealed class BuildingIncomeFeedbackManager : MonoBehaviour
    {
        private sealed class BuildingVisualState
        {
            public BuildingInstance Instance;
            public float Interval;
            public float NextPopupTime;
        }

        [Header("References")]
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private BuildingIncomePopupPool popupPool;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private PopulationManager populationManager;

        [Header("Timing")]
        [SerializeField] private Vector2 visualIntervalRange = new Vector2(0.5f, 1.5f);
        [SerializeField, Min(0.02f)] private float visualCheckInterval = 0.1f;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.15f, 0f);

        private readonly List<BuildingVisualState> states = new List<BuildingVisualState>(64);
        private readonly Dictionary<string, BuildingVisualState> statesById = new Dictionary<string, BuildingVisualState>();
        private GridMapData observedMap;
        private float nextVisualCheckTime;

        private void Start()
        {
            if (upgradeManager == null)
            {
                upgradeManager = UpgradeManager.Instance;
            }

            if (populationManager == null)
            {
                populationManager = PopulationManager.Instance;
            }

            TryBindMap();
        }

        private void Update()
        {
            if (observedMap != (mapRenderer != null ? mapRenderer.MapData : null))
            {
                TryBindMap();
            }

            if (observedMap == null || worldCamera == null || popupPool == null)
            {
                return;
            }

            float currentTime = Time.time;
            if (currentTime < nextVisualCheckTime)
            {
                return;
            }

            nextVisualCheckTime = currentTime + Mathf.Max(0.02f, visualCheckInterval);
            for (int i = 0; i < states.Count; i++)
            {
                BuildingVisualState state = states[i];
                if (currentTime < state.NextPopupTime)
                {
                    continue;
                }

                TryShowPopup(state);
                ScheduleNext(state, currentTime);
            }
        }

        private void OnDisable()
        {
            UnbindMap();
        }

        private void TryBindMap()
        {
            GridMapData mapData = mapRenderer != null ? mapRenderer.MapData : null;
            if (observedMap == mapData)
            {
                return;
            }

            UnbindMap();
            observedMap = mapData;
            if (observedMap == null)
            {
                return;
            }

            observedMap.BuildingPlaced += AddBuilding;
            observedMap.BuildingRemoved += RemoveBuilding;
            foreach (BuildingInstance instance in observedMap.BuildingInstances)
            {
                AddBuilding(instance);
            }
        }

        private void UnbindMap()
        {
            if (observedMap != null)
            {
                observedMap.BuildingPlaced -= AddBuilding;
                observedMap.BuildingRemoved -= RemoveBuilding;
            }

            observedMap = null;
            states.Clear();
            statesById.Clear();
        }

        private void AddBuilding(BuildingInstance instance)
        {
            if (instance == null || instance.Definition == null
                || instance.GetProductionPerSecond() <= 0d
                || (instance.Definition.buildingRole != BuildingRole.None
                    && instance.Definition.buildingRole != BuildingRole.Workplace)
                || statesById.ContainsKey(instance.InstanceId))
            {
                return;
            }

            float interval = GetRandomInterval();
            BuildingVisualState state = new BuildingVisualState
            {
                Instance = instance,
                Interval = interval,
                NextPopupTime = Time.time + UnityEngine.Random.Range(0f, interval)
            };

            states.Add(state);
            statesById.Add(instance.InstanceId, state);
        }

        private void RemoveBuilding(BuildingInstance instance)
        {
            if (instance == null || !statesById.TryGetValue(instance.InstanceId, out BuildingVisualState state))
            {
                return;
            }

            statesById.Remove(instance.InstanceId);
            states.Remove(state);
        }

        private void TryShowPopup(BuildingVisualState state)
        {
            BuildingInstance instance = state.Instance;
            if (instance == null || instance.Definition == null
                || !mapRenderer.TryGetVisibleBuildingWorldPosition(instance, out Vector3 worldPosition))
            {
                return;
            }

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition + worldOffset);
            if (screenPosition.z <= 0f
                || screenPosition.x < 0f || screenPosition.x > Screen.width
                || screenPosition.y < 0f || screenPosition.y > Screen.height)
            {
                return;
            }

            double cityEfficiencyMultiplier = upgradeManager == null
                ? 1d
                : upgradeManager.GetCityEfficiencyMultiplier();
            double employmentMultiplier = populationManager == null
                ? 1d
                : populationManager.GetProductionMultiplier(instance.Definition);
            double production = instance.GetProductionPerSecond()
                * employmentMultiplier
                * cityEfficiencyMultiplier;
            if (production <= 0d)
            {
                return;
            }
            long amount = ToDisplayAmount(production);
            popupPool.Show(worldPosition + worldOffset, worldCamera, amount, production);
        }

        private void ScheduleNext(BuildingVisualState state, float currentTime)
        {
            state.Interval = GetRandomInterval();
            state.NextPopupTime = currentTime + state.Interval;
        }

        private float GetRandomInterval()
        {
            float minimum = Mathf.Max(0.05f, Mathf.Min(visualIntervalRange.x, visualIntervalRange.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(visualIntervalRange.x, visualIntervalRange.y));
            return UnityEngine.Random.Range(minimum, maximum);
        }

        private static long ToDisplayAmount(double amount)
        {
            if (amount <= 0d)
            {
                return 0L;
            }

            return (long)Math.Min(long.MaxValue, Math.Max(1d, Math.Round(amount, MidpointRounding.AwayFromZero)));
        }
    }
}
