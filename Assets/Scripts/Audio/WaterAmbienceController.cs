using LoadGame.Map;
using UnityEngine;

namespace LoadGame.Audio
{
    public sealed class WaterAmbienceController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private ChunkedGridMapRenderer mapRenderer;

        [Header("Sampling")]
        [SerializeField, Min(1)] private int horizontalSamples = 3;
        [SerializeField, Min(1)] private int verticalSamples = 3;
        [SerializeField, Range(0f, 0.45f)] private float viewportPadding = 0.12f;
        [SerializeField, Min(0.05f)] private float checkInterval = 0.25f;
        [SerializeField, Range(0f, 1f)] private float minimumWaterCoverage = 0.05f;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float maximumVolume = 0.35f;
        [SerializeField, Min(0.01f)] private float fadeInDuration = 1.5f;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 2f;
        [SerializeField, Min(0.1f)] private float coverageResponse = 0.75f;

        private float checkTimer;
        private float currentLevel;
        private float targetLevel;
        private bool ambientStarted;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (mapRenderer == null)
            {
                mapRenderer = FindFirstObjectByType<ChunkedGridMapRenderer>();
            }
        }

        private void OnEnable()
        {
            checkTimer = 0f;
            TryStartAmbient();
        }

        private void Update()
        {
            if (!ambientStarted)
            {
                TryStartAmbient();
            }

            checkTimer -= Time.unscaledDeltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = Mathf.Max(0.05f, checkInterval);
                targetLevel = CalculateTargetLevel();
            }

            float duration = targetLevel > currentLevel ? fadeInDuration : fadeOutDuration;
            float step = Mathf.Max(0.01f, maximumVolume) * Time.unscaledDeltaTime
                / Mathf.Max(0.01f, duration);
            currentLevel = Mathf.MoveTowards(currentLevel, targetLevel, step);
            SoundManager.Instance?.SetAmbientLevel(currentLevel);
        }

        private void OnDisable()
        {
            currentLevel = 0f;
            targetLevel = 0f;
            SoundManager.Instance?.SetAmbientLevel(0f);
        }

        private void TryStartAmbient()
        {
            SoundManager manager = SoundManager.Instance;
            ambientStarted = manager != null && manager.StartAmbient(SoundId.WaterAmbience);
        }

        private float CalculateTargetLevel()
        {
            if (targetCamera == null || mapRenderer == null)
            {
                return 0f;
            }

            int sampleCountX = Mathf.Max(1, horizontalSamples);
            int sampleCountY = Mathf.Max(1, verticalSamples);
            int waterCount = 0;
            int validCount = 0;
            float padding = Mathf.Clamp(viewportPadding, 0f, 0.45f);
            float viewportRange = 1f - padding * 2f;
            float mapPlaneDistance = Mathf.Abs(targetCamera.transform.position.z - mapRenderer.transform.position.z);

            for (int y = 0; y < sampleCountY; y++)
            {
                float viewportY = padding + viewportRange * ((y + 0.5f) / sampleCountY);
                for (int x = 0; x < sampleCountX; x++)
                {
                    float viewportX = padding + viewportRange * ((x + 0.5f) / sampleCountX);
                    Vector3 worldPosition = targetCamera.ViewportToWorldPoint(
                        new Vector3(viewportX, viewportY, mapPlaneDistance));
                    if (!mapRenderer.TryGetVisibleCellAtWorld(worldPosition, out GridCellData cell))
                    {
                        continue;
                    }

                    validCount++;
                    if (cell.CellType == GridCellType.Water)
                    {
                        waterCount++;
                    }
                }
            }

            if (validCount == 0)
            {
                return 0f;
            }

            float coverage = (float)waterCount / validCount;
            float normalizedCoverage = Mathf.InverseLerp(minimumWaterCoverage, 1f, coverage);
            return Mathf.Pow(normalizedCoverage, Mathf.Max(0.1f, coverageResponse)) * maximumVolume;
        }
    }
}
