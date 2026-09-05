using UnityEngine;
using LoadGame.Economy;

namespace LoadGame.Map
{
    public sealed class MapManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string mapResourcePath = "test_map";

        [Header("Scene References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;
        [SerializeField] private DragPanCamera dragPanCamera;
        [SerializeField] private GridCellSelectionController selectionController;
        [SerializeField] private CellInfoPopup cellInfoPopup;
        [SerializeField] private EconomyManager economyManager;

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

            Debug.Log("MapManager initialized map renderer, camera pan, selection, and popup.", this);
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
