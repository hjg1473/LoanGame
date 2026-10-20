using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using LoadGame.Audio;
using LoadGame.Economy;
using LoadGame.UI;
using System;
using System.Collections.Generic;

namespace LoadGame.Map
{
    public sealed class GridCellSelectionController : MonoBehaviour
    {
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;
        [SerializeField] private CellInfoPopup popup;
        [SerializeField] private float clickMoveThreshold = 8f;
        [SerializeField] private long startingLandPrice = 100L;
        [SerializeField] private double landPriceGrowthRate = 0.1d;
        [SerializeField] private EconomyManager economyManager;
        [Header("Unavailable Land Feedback")]
        [SerializeField] private CellPurchaseFeedbackPool purchaseFeedbackPool;
        [SerializeField] private string unavailableLandMessage = "This land cannot be purchased.";
        [SerializeField] private bool debugInput;

        private Camera targetCamera;
        private Vector2 pointerDownPosition;
        private bool pointerDown;
        private bool isDraggingPointer;
        private bool pointerStartedOverUi;
        private GridCellData selectedCell;
        private BuildingDefinition[] buildingDefinitions;
        private long currentLandPrice;

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
            currentLandPrice = Math.Max(1L, startingLandPrice);
            if (string.IsNullOrWhiteSpace(unavailableLandMessage)
                || unavailableLandMessage == "구매할 수 없는 땅입니다")
            {
                unavailableLandMessage = "This land cannot be purchased.";
            }
        }

        private void Start()
        {
            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<ChunkedGridMapRenderer>();
            }

            if (popup == null)
            {
                popup = FindFirstObjectByType<CellInfoPopup>();
            }

            if (economyManager == null)
            {
                economyManager = EconomyManager.Instance;
            }

            if (purchaseFeedbackPool == null)
            {
                purchaseFeedbackPool = FindFirstObjectByType<CellPurchaseFeedbackPool>();
            }

            if (mapRenderer != null)
            {
                buildingDefinitions = mapRenderer.BuildingDefinitions;
            }
        }

        private void Update()
        {
            if (mapRenderer == null || popup == null || targetCamera == null)
            {
                return;
            }

            if (!TryGetPointerState(out Vector2 pointerPosition, out bool pressedThisFrame, out bool releasedThisFrame))
            {
                return;
            }

            if (economyManager != null && economyManager.IsBankrupt())
            {
                ClearSelection();
                return;
            }

            if (pressedThisFrame)
            {
                pointerDown = true;
                isDraggingPointer = false;
                pointerStartedOverUi = IsPointerOverUi(pointerPosition);
                pointerDownPosition = pointerPosition;
                if (debugInput)
                {
                    Debug.Log($"Selection pointer down. screen={pointerDownPosition}, overUi={pointerStartedOverUi}", this);
                }
            }

            if (pointerDown && Vector2.Distance(pointerDownPosition, pointerPosition) > clickMoveThreshold)
            {
                isDraggingPointer = true;
            }

            if (!pointerDown || !releasedThisFrame)
            {
                return;
            }

            pointerDown = false;
            Vector2 pointerUpPosition = pointerPosition;
            bool pointerEndedOverUi = IsPointerOverUi(pointerUpPosition);
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
                ClearSelection();
                return;
            }

            if (!mapRenderer.IsCellRevealed(cell))
            {
                if (debugInput)
                {
                    Debug.Log($"Selection ignored unrevealed cell. x={cell.x}, y={cell.y}", this);
                }

                if (!cell.owned)
                {
                    ShowUnavailableLandFeedback(cell);
                }
                else
                {
                    ClearSelection();
                }
                return;
            }

            if (mapRenderer.IsRoadVisible(cell))
            {
                selectedCell = cell;
                mapRenderer.HighlightCell(cell);
                if (mapRenderer.TryGetBuildingInstanceAtCell(cell, out BuildingInstance roadInstance)
                    && roadInstance != null)
                {
                    popup.ShowInstalledBuilding(cell, roadInstance, GetBuildingIcon(roadInstance.Definition));
                }
                else
                {
                    popup.ShowRoad(cell);
                }

                return;
            }

            if (cell.CellType != GridCellType.Empty || mapRenderer.IsRoad(cell))
            {
                if (debugInput)
                {
                    Debug.Log($"Selection rejected non-purchasable terrain. x={cell.x}, y={cell.y}, type={cell.type}, owned={cell.owned}", this);
                }

                ShowUnavailableLandFeedback(cell);
                return;
            }


            if (!cell.owned && !CanBuy(cell))
            {
                ShowUnavailableLandFeedback(cell);
                return;
            }

            if (debugInput)
            {
                Debug.Log($"Selection accepted cell. x={cell.x}, y={cell.y}, type={cell.type}, owned={cell.owned}", this);
            }

            selectedCell = cell;
            mapRenderer.HighlightCell(cell);
            if (mapRenderer.TryGetBuildingInstanceAtCell(cell, out BuildingInstance installedInstance) && installedInstance != null)
            {
                popup.ShowInstalledBuilding(cell, installedInstance, GetBuildingIcon(installedInstance.Definition));
                return;
            }

            popup.Show(cell, currentLandPrice, CanBuy(cell), buildingDefinitions, CanPlaceSelectedBuilding, GetBuildingIcon);
        }

        public void TryBuySelectedCell()
        {
            if (selectedCell == null || mapRenderer == null || economyManager == null)
            {
                return;
            }

            if (economyManager.IsBankrupt())
            {
                return;
            }

            if (!CanBuy(selectedCell))
            {
                popup.Show(selectedCell, currentLandPrice, canBuy: false);
                return;
            }

            economyManager.Pay(currentLandPrice);
            mapRenderer.SetCellOwned(selectedCell, true);
            SoundManager.Instance?.PlaySFX(SoundId.LandPurchase);
            IncreaseLandPrice();
            popup.Show(selectedCell, currentLandPrice, canBuy: false, buildingDefinitions, CanPlaceSelectedBuilding, GetBuildingIcon);
        }

        public void TryBuildSelectedCell(int buildingIndex)
        {
            if (selectedCell == null || mapRenderer == null || economyManager == null || !IsValidBuildingIndex(buildingIndex))
            {
                return;
            }

            if (economyManager.IsBankrupt())
            {
                return;
            }

            BuildingDefinition buildingDefinition = buildingDefinitions[buildingIndex];
            if (!mapRenderer.CanPlaceBuilding(buildingDefinition, selectedCell))
            {
                SoundManager.Instance?.PlaySFX(SoundId.BuildingFail);
                popup.Show(selectedCell, currentLandPrice, CanBuy(selectedCell), buildingDefinitions, CanPlaceSelectedBuilding, GetBuildingIcon);
                return;
            }

            economyManager.Pay(buildingDefinition.price);
            BuildingInstance instance = mapRenderer.PlaceBuilding(selectedCell, buildingDefinition);
            if (instance == null)
            {
                SoundManager.Instance?.PlaySFX(SoundId.BuildingFail);
                popup.Show(selectedCell, currentLandPrice, CanBuy(selectedCell), buildingDefinitions, CanPlaceSelectedBuilding, GetBuildingIcon);
                return;
            }

            SoundManager.Instance?.PlaySFX(SoundId.BuildingPlace);
            popup.ShowInstalledBuilding(selectedCell, instance, GetBuildingIcon(buildingDefinition));
        }

        public void TryRemoveSelectedRoad()
        {
            TryRemoveSelectedObject();
        }

        public void TryRemoveSelectedObject()
        {
            if (selectedCell == null || mapRenderer == null || economyManager == null || economyManager.IsBankrupt())
            {
                return;
            }

            if (!mapRenderer.CanRemovePlacedObject(selectedCell))
            {
                return;
            }

            long refund = mapRenderer.GetDemolitionRefund(selectedCell);
            if (mapRenderer.RemovePlacedObject(selectedCell))
            {
                if (refund > 0L)
                {
                    economyManager.AddCash(refund);
                }
                popup.Hide();
            }
        }

        private bool CanBuy(GridCellData cell)
        {
            return cell != null
                && cell.CellType == GridCellType.Empty
                && !cell.owned
                && mapRenderer != null
                && mapRenderer.CanPurchaseLand(cell)
                && economyManager != null;
        }

        private bool CanBuildOnCell(GridCellData cell)
        {
            return cell != null
                && economyManager != null
                && buildingDefinitions != null
                && HasAnyPlaceableBuilding(cell);
        }

        private bool CanPlaceSelectedBuilding(BuildingDefinition definition)
        {
            return selectedCell != null
                && mapRenderer != null
                && definition != null
                && mapRenderer.CanPlaceBuilding(definition, selectedCell);
        }

        private bool HasAnyPlaceableBuilding(GridCellData cell)
        {
            if (mapRenderer == null || buildingDefinitions == null)
            {
                return false;
            }

            for (int i = 0; i < buildingDefinitions.Length; i++)
            {
                if (buildingDefinitions[i] != null && mapRenderer.CanPlaceBuilding(buildingDefinitions[i], cell))
                {
                    return true;
                }
            }

            return false;
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

        private void IncreaseLandPrice()
        {
            double multiplier = 1d + Math.Max(0d, landPriceGrowthRate);
            currentLandPrice = (long)Math.Min(long.MaxValue, Math.Round(currentLandPrice * multiplier, MidpointRounding.AwayFromZero));
        }

        private void ShowUnavailableLandFeedback(GridCellData cell)
        {
            selectedCell = null;
            popup.Hide();
            mapRenderer.ClearBuildingFade();
            mapRenderer.ShowPurchasableLandHint();
            SoundManager.Instance?.PlaySFX(SoundId.LandUnavailable);
            if (purchaseFeedbackPool != null)
            {
                purchaseFeedbackPool.Show(
                    unavailableLandMessage,
                    mapRenderer.GetCellCenterWorld(cell.x, cell.y),
                    targetCamera);
            }
        }

        private Vector3 ScreenToWorldOnMapPlane(Vector2 screenPosition)
        {
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z);
            return targetCamera.ScreenToWorldPoint(screenPoint);
        }

        private static bool TryGetPointerState(
            out Vector2 position,
            out bool pressedThisFrame,
            out bool releasedThisFrame)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                {
                    position = touch.position.ReadValue();
                    pressedThisFrame = touch.press.wasPressedThisFrame;
                    releasedThisFrame = touch.press.wasReleasedThisFrame;
                    return true;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
                releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
                return true;
            }

            position = default;
            pressedThisFrame = false;
            releasedThisFrame = false;
            return false;
        }

        private static bool IsPointerOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            PointerEventData eventData = new PointerEventData(eventSystem)
            {
                position = screenPosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            eventSystem.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        public void ClearSelection()
        {
            selectedCell = null;
            if (mapRenderer != null)
            {
                mapRenderer.ClearHighlight();
                mapRenderer.ClearBuildingFade();
            }
        }
    }
}
