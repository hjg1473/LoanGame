using UnityEngine;
using UnityEngine.InputSystem;

namespace LoadGame.Map
{
    [RequireComponent(typeof(Camera))]
    public sealed class DragPanCamera : MonoBehaviour
    {
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;
        [SerializeField] private float dragSpeed = 1f;
        [SerializeField] private float zoomStep = 5f;
        [SerializeField] private float minZoomZ = -10f;
        [SerializeField] private float maxZoomZ = -40f;
        [SerializeField] private bool debugInput;

        private Camera cameraComponent;
        private Vector3 lastPointerWorld;
        private bool isDragging;
        private Rect cameraBounds;

        public void Initialize(ChunkedGridMapRenderer renderer, GridMapData mapData)
        {
            if (cameraComponent == null)
            {
                cameraComponent = GetComponent<Camera>();
            }

            mapRenderer = renderer;
            cameraBounds = mapData.CameraBounds.ToWorldBounds(mapRenderer.TileWidth, mapRenderer.TileHeight);
            if (debugInput)
            {
                Debug.Log($"DragPanCamera initialized. bounds={cameraBounds}, cameraPos={transform.position}", this);
            }

            ClampToBounds();
        }

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
        }

        private void Start()
        {
            if (mapRenderer == null)
            {
                mapRenderer = FindObjectOfType<ChunkedGridMapRenderer>();
            }

            if (mapRenderer != null && mapRenderer.MapData != null)
            {
                Initialize(mapRenderer, mapRenderer.MapData);
            }
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastPointerWorld = ScreenToWorldOnMapPlane(mouse.position.ReadValue());
                if (debugInput)
                {
                    Debug.Log($"Drag down. screen={mouse.position.ReadValue()}, world={lastPointerWorld}", this);
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
                if (debugInput)
                {
                    Debug.Log($"Drag up. screen={mouse.position.ReadValue()}, cameraPos={transform.position}", this);
                }
            }

            if (!isDragging)
            {
                return;
            }

            Vector3 pointerWorld = ScreenToWorldOnMapPlane(mouse.position.ReadValue());
            Vector3 delta = lastPointerWorld - pointerWorld;
            transform.position += delta * dragSpeed;
            ClampToBounds();

            if (debugInput && delta.sqrMagnitude > 0.0001f)
            {
                Debug.Log($"Dragging. pointerWorld={pointerWorld}, delta={delta}, cameraPos={transform.position}", this);
            }
        }

        private Vector3 ScreenToWorldOnMapPlane(Vector2 screenPosition)
        {
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, -cameraComponent.transform.position.z);
            return cameraComponent.ScreenToWorldPoint(screenPoint);
        }

        private void ClampToBounds()
        {
            if (cameraBounds.width <= 0f || cameraBounds.height <= 0f)
            {
                return;
            }

            Vector3 position = transform.position;
            float halfHeight = cameraComponent.orthographicSize;
            float halfWidth = halfHeight * cameraComponent.aspect;

            position.x = ClampAxis(position.x, cameraBounds.xMin + halfWidth, cameraBounds.xMax - halfWidth);
            position.y = ClampAxis(position.y, cameraBounds.yMin + halfHeight, cameraBounds.yMax - halfHeight);
            transform.position = position;
        }

        public void ZoomIn()
        {
            SetCameraZ(transform.position.z + zoomStep);
        }

        public void ZoomOut()
        {
            SetCameraZ(transform.position.z - zoomStep);
        }

        private void SetCameraZ(float z)
        {
            Vector3 position = transform.position;
            position.z = Mathf.Clamp(z, maxZoomZ, minZoomZ);
            transform.position = position;

            if (debugInput)
            {
                Debug.Log($"Camera zoom changed. z={position.z}", this);
            }
        }

        private static float ClampAxis(float value, float min, float max)
        {
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }
    }
}
