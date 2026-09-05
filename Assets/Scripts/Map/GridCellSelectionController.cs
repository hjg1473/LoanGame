using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using LoadGame.Economy;

namespace LoadGame.Map
{
    public sealed class GridCellSelectionController : MonoBehaviour
    {
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;
        [SerializeField] private CellInfoPopup popup;
        [SerializeField] private float clickMoveThreshold = 8f;
        [SerializeField] private long landPrice = 100L;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private bool debugInput;

        private Camera targetCamera;
        private Vector2 pointerDownPosition;
        private bool pointerDown;
        private bool isDraggingPointer;
        private bool pointerStartedOverUi;
        private GridCellData selectedCell;
        private BuildingDefinition[] buildingDefinitions;

        public void Initialize(ChunkedGridMapRenderer renderer, CellInfoPopup cellPopup, Camera camera, EconomyManager economy)
        {
            mapRenderer = renderer;
            popup = cellPopup;
            targetCamera = camera;
            economyManager = economy;
            buildingDefinitions = mapRenderer.BuildingDefinitions;
            if (debugInput)
            {
                Debug.Log($"GridCellSelectionController initialized. camera={targetCamera?.name}, renderer={mapRenderer?.name}, popup={popup?.name}", this);
            }
        }

        private void Awake()
        {
            targetCamera = Camera.main;
        }

        private void Start()
        {
            if (mapRenderer == null)
            {
                mapRenderer = FindObjectOfType<ChunkedGridMapRenderer>();
            }

            if (popup == null)
            {
                popup = FindObjectOfType<CellInfoPopup>();
            }

            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (mapRenderer != null)
            {
                buildingDefinitions = mapRenderer.BuildingDefinitions;
            }
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || mapRenderer == null || popup == null || targetCamera == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                pointerDown = true;
                isDraggingPointer = false;
                pointerStartedOverUi = IsPointerOverUi();
                pointerDownPosition = mouse.position.ReadValue();
                if (debugInput)
                {
                    Debug.Log($"Selection pointer down. screen={pointerDownPosition}, overUi={pointerStartedOverUi}", this);
                }
            }

            if (pointerDown && Vector2.Distance(pointerDownPosition, mouse.position.ReadValue()) > clickMoveThreshold)
            {
                isDraggingPointer = true;
            }

            if (!pointerDown || !mouse.leftButton.wasReleasedThisFrame)
            {
                return;
            }

            pointerDown = false;
            Vector2 pointerUpPosition = mouse.position.ReadValue();
            bool pointerEndedOverUi = IsPointerOverUi();
            if (isDraggingPointer || Vector2.Distance(pointerDownPosition, pointerUpPosition) > clickMoveThreshold)
            {
                if (debugInput)
                {
                    Debug.Log($"Selection ignored as drag. down={pointerDownPosition}, up={pointerUpPosition}, distance={Vector2.Distance(pointerDownPosition, pointerUpPosition)}", this);
                }
                return;
            }

            if (pointerStartedOverUi || pointerEndedOverUi || popup.ContainsScreenPoint(pointerUpPosition, null))
            {
                if (debugInput)
                {
                    Debug.Log($"Selection ignored over UI. screen={pointerUpPosition}, downOverUi={pointerStartedOverUi}, upOverUi={pointerEndedOverUi}", this);
                }
                return;
            }

            Vector3 worldPosition = ScreenToWorldOnMapPlane(pointerUpPosition);
            if (debugInput)
            {
                Debug.Log($"Selection click candidate. screen={pointerUpPosition}, world={worldPosition}", this);
            }

            if (!mapRenderer.TryGetVisibleCellAtWorld(worldPosition, out GridCellData cell))
            {
                if (debugInput)
                {
                    Debug.Log($"Selection found no visible tile. world={worldPosition}", this);
                }
                return;
            }

            if (cell.CellType == GridCellType.Road)
            {
                if (debugInput)
                {
                    Debug.Log($"Selection ignored road cell. x={cell.x}, y={cell.y}, type={cell.type}, owned={cell.owned}", this);
                }
                return;
            }

            if (debugInput)
            {
                Debug.Log($"Selection accepted cell. x={cell.x}, y={cell.y}, type={cell.type}, owned={cell.owned}", this);
            }

            selectedCell = cell;
            mapRenderer.HighlightCell(cell);
            if (!string.IsNullOrEmpty(cell.buildingId) && mapRenderer.TryGetBuildingDefinition(cell.buildingId, out BuildingDefinition installedBuilding))
            {
                popup.ShowInstalledBuilding(cell, installedBuilding);
                return;
            }

            popup.Show(cell, landPrice, CanBuy(cell), buildingDefinitions, CanBuildOnCell(cell), GetBuildingIcon);
        }

        public void TryBuySelectedCell()
        {
            if (selectedCell == null || mapRenderer == null || economyManager == null)
            {
                return;
            }

            if (!CanBuy(selectedCell))
            {
                popup.Show(selectedCell, landPrice, canBuy: false);
                return;
            }

            economyManager.Pay(landPrice);
            mapRenderer.SetCellOwned(selectedCell, true);
            popup.Show(selectedCell, landPrice, canBuy: false, buildingDefinitions, CanBuildOnCell(selectedCell), GetBuildingIcon);
        }

        public void TryBuildSelectedCell(int buildingIndex)
        {
            if (selectedCell == null || mapRenderer == null || economyManager == null || !IsValidBuildingIndex(buildingIndex))
            {
                return;
            }

            BuildingDefinition buildingDefinition = buildingDefinitions[buildingIndex];
            if (!CanBuildOnCell(selectedCell))
            {
                popup.Show(selectedCell, landPrice, CanBuy(selectedCell), buildingDefinitions, false, GetBuildingIcon);
                return;
            }

            economyManager.Pay(buildingDefinition.price);
            mapRenderer.SetCellBuilding(selectedCell, buildingDefinition.id);
            economyManager.RegisterProduction(buildingDefinition.cashPerSecond);
            popup.ShowInstalledBuilding(selectedCell, buildingDefinition);
        }

        private bool CanBuy(GridCellData cell)
        {
            return cell != null
                && cell.CellType == GridCellType.Empty
                && !cell.owned
                && economyManager != null;
        }

        private bool CanBuildOnCell(GridCellData cell)
        {
            return cell != null
                && cell.owned
                && cell.CellType == GridCellType.Empty
                && string.IsNullOrEmpty(cell.buildingId)
                && economyManager != null;
        }

        private bool IsValidBuildingIndex(int buildingIndex)
        {
            return buildingDefinitions != null
                && buildingIndex >= 0
                && buildingIndex < buildingDefinitions.Length
                && buildingDefinitions[buildingIndex] != null;
        }

        private Sprite GetBuildingIcon(BuildingDefinition definition)
        {
            return mapRenderer == null ? null : mapRenderer.LoadBuildingIcon(definition);
        }

        private Vector3 ScreenToWorldOnMapPlane(Vector2 screenPosition)
        {
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z);
            return targetCamera.ScreenToWorldPoint(screenPoint);
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public void ClearSelection()
        {
            selectedCell = null;
            if (mapRenderer != null)
            {
                mapRenderer.ClearHighlight();
            }
        }
    }
}
