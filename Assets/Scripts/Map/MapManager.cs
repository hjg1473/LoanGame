using UnityEngine;
using LoadGame.Economy;
using LoadGame.UI;

namespace LoadGame.Map
{
    public sealed class MapManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string mapResourcePath = "city_map";

        [Header("Scene References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;
        [SerializeField] private DragPanCamera dragPanCamera;
        [SerializeField] private GridCellSelectionController selectionController;
        [SerializeField] private CellInfoPopup cellInfoPopup;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private PopulationManager populationManager;
        [SerializeField] private CityValueManager cityValueManager;
        [SerializeField] private StockMarketManager stockMarketManager;
        [SerializeField] private NetWorthManager netWorthManager;
        [SerializeField] private CashDisplay cashDisplay;
        [SerializeField] private DevelopmentProjectManager developmentProjectManager;
        [SerializeField] private ProjectPanelUI projectPanelUI;
        [SerializeField] private SaveGameManager saveGameManager;

        private GridMapData mapData;

        public GridMapData MapData => mapData;

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (targetCamera == null || mapRenderer == null || dragPanCamera == null || selectionController == null || cellInfoPopup == null || economyManager == null)
            {
                Debug.LogError("MapManager scene references are not fully assigned.", this);
                enabled = false;
                return;
            }

            mapData = GridMapLoader.LoadFromResources(mapResourcePath);

            mapRenderer.Initialize(mapData, targetCamera);
            dragPanCamera.Initialize(mapRenderer, mapData);
            selectionController.Initialize(mapRenderer, cellInfoPopup, targetCamera, economyManager);
            cellInfoPopup.SetSelectionController(selectionController);
            cellInfoPopup.Hide();

            if (populationManager == null)
            {
                populationManager = FindFirstObjectByType<PopulationManager>();
            }

            if (populationManager == null)
            {
                populationManager = gameObject.AddComponent<PopulationManager>();
            }

            populationManager.Initialize(mapData, economyManager);

            if (cityValueManager == null)
            {
                cityValueManager = FindFirstObjectByType<CityValueManager>();
            }

            if (cityValueManager == null)
            {
                cityValueManager = gameObject.AddComponent<CityValueManager>();
            }

            if (cityValueManager != null)
            {
                cityValueManager.Initialize(mapData, mapRenderer, economyManager);
            }

            if (stockMarketManager == null)
            {
                stockMarketManager = FindFirstObjectByType<StockMarketManager>();
            }

            if (stockMarketManager != null)
            {
                stockMarketManager.SetCityValueManager(cityValueManager);
            }

            if (netWorthManager == null)
            {
                netWorthManager = FindFirstObjectByType<NetWorthManager>();
            }

            if (netWorthManager == null)
            {
                netWorthManager = gameObject.AddComponent<NetWorthManager>();
            }

            netWorthManager.Initialize(economyManager, cityValueManager, stockMarketManager);

            if (cashDisplay == null)
            {
                cashDisplay = FindFirstObjectByType<CashDisplay>();
            }

            if (cashDisplay != null)
            {
                cashDisplay.SetValueManagers(cityValueManager, netWorthManager);
                cashDisplay.SetPopulationManager(populationManager);
            }

            if (developmentProjectManager == null)
            {
                developmentProjectManager = FindFirstObjectByType<DevelopmentProjectManager>();
            }

            if (developmentProjectManager == null)
            {
                developmentProjectManager = gameObject.AddComponent<DevelopmentProjectManager>();
            }

            developmentProjectManager.Initialize(mapData, mapRenderer, economyManager, cityValueManager);

            if (projectPanelUI == null)
            {
                projectPanelUI = FindFirstObjectByType<ProjectPanelUI>();
            }

            if (projectPanelUI != null)
            {
                projectPanelUI.SetProjectManager(developmentProjectManager);
            }

            if (saveGameManager == null)
            {
                saveGameManager = FindFirstObjectByType<SaveGameManager>();
            }
            if (saveGameManager == null)
            {
                saveGameManager = gameObject.AddComponent<SaveGameManager>();
            }
            saveGameManager.Initialize(mapData, mapRenderer, economyManager, populationManager, developmentProjectManager);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("MapManager initialized map renderer, camera pan, selection, and popup.", this);
#endif
        }

        private void OnValidate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }
    }
}
