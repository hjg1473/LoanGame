using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LoadGame.Map
{
    public sealed class ChunkedGridMapRenderer : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Camera targetCamera;

        [Header("Chunking")]
        [SerializeField] private int visibleChunkRadius = 2;
        [SerializeField, Min(1)] private int chunkTransitionsPerFrame = 1;
        [SerializeField] private ProceduralRoadNetwork proceduralRoadNetwork = new ProceduralRoadNetwork();

        [Header("World Growth")]
        [SerializeField] private WorldGenerationSettings worldSettings = new WorldGenerationSettings();
        [SerializeField] private Sprite waterSprite;
        [SerializeField] private Sprite[] grassVariantSprites = System.Array.Empty<Sprite>();

        [Header("Water Shoreline Foam")]
        [SerializeField] private Tilemap waterFoamTilemap;
        [SerializeField] private Material waterFoamMaterial;

        [Header("Nature Decorations")]
        [SerializeField] private Transform decorationRoot;
        [SerializeField] private WorldDecorationDefinition[] decorationDefinitions = System.Array.Empty<WorldDecorationDefinition>();
        [SerializeField] private int decorationSortingOrderBase = 500;
        [SerializeField] private Material treeWindMaterial;

        [Header("Road Reveal Effect")]
        [SerializeField] private ParticleSystem roadRevealParticlePrefab;
        [SerializeField, Min(0)] private int roadEffectPoolSize = 8;
        [SerializeField, Min(1)] private int roadRevealBatchSize = 2;

        [Header("Tilemaps")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private Tilemap buildingTilemap;
        [SerializeField] private Tilemap highlightTilemap;
        [SerializeField] private Tilemap groundDecorationTilemap;
        [SerializeField] private Transform buildingRoot;
        [SerializeField] private Material buildingMaterial;
        [SerializeField] private int buildingSortingOrderBase = 1000;
        [SerializeField] private int buildingSortingOrderStep = 10;
        [SerializeField] private int buildingSortingOrderOffset = 1;
        [SerializeField] private Vector3 buildingWorldOffset;
        [SerializeField] private BuildingPlacementFeedback buildingPlacementFeedback;

        [Header("Tile Size")]
        [SerializeField] private float tileWidth = 1f;
        [SerializeField] private float tileHeight = 0.5f;

        [Header("Sprites")]
        [SerializeField] private string emptySpritePath = "Sprites/Grass1";
        [SerializeField] private string roadSpritePath = "Sprites/Load1";
        [SerializeField] private string ownedSpritePath = "Sprites/Building1";
        [SerializeField] private string highlightSpritePath = "Sprites/Highlight1";
        [Header("Purchasable Land Hint")]
        [SerializeField] private Color purchasableHintColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField, Min(0.1f)] private float purchasableHintDuration = 1.2f;
        [SerializeField, Range(0f, 1f)] private float buildingAlpha = 1f;
        [Header("Building Visibility")]
        [SerializeField] private bool fadeBuildingsAroundSelectedCell = true;
        [SerializeField, Min(0)] private int selectedCellFadeRadius = 1;
        [SerializeField, Range(0f, 1f)] private float selectedAreaBuildingAlpha = 0.45f;
        [SerializeField] private BuildingDatabase buildingDatabase;
        [SerializeField] private BuildingQualitySettings buildingQualitySettings;
        [SerializeField] private RoadVisualDefinition[] roadVisualDefinitions;
        [SerializeField] private bool debugMap;

        private readonly HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> neededChunksBuffer = new HashSet<Vector2Int>();
        private readonly Queue<Vector2Int> pendingChunkLoads = new Queue<Vector2Int>();
        private readonly Queue<Vector2Int> pendingChunkUnloads = new Queue<Vector2Int>();
        private readonly Dictionary<int, TileBase> roadTilesByMask = new Dictionary<int, TileBase>();
        private readonly Dictionary<string, TileBase> buildingTiles = new Dictionary<string, TileBase>();
        private readonly Dictionary<string, Sprite> buildingSprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> buildingSpritesByPath = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, TileBase> buildingGroundTilesByPath = new Dictionary<string, TileBase>();
        private readonly Dictionary<string, BuildingDefinition> buildingDefinitionsById = new Dictionary<string, BuildingDefinition>();
        private readonly HashSet<Vector3Int> activeBuildingCells = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, SpriteRenderer> activeBuildingRenderers = new Dictionary<Vector3Int, SpriteRenderer>();
        private readonly Dictionary<WorldDecorationType, WorldDecorationDefinition> decorationDefinitionsByType = new Dictionary<WorldDecorationType, WorldDecorationDefinition>();
        private readonly Dictionary<Sprite, TileBase> groundDecorationTilesBySprite = new Dictionary<Sprite, TileBase>();
        private readonly Dictionary<Vector2Int, ActiveDecoration> activeDecorations = new Dictionary<Vector2Int, ActiveDecoration>();
        private readonly Queue<ActiveDecoration> decorationPool = new Queue<ActiveDecoration>();
        private readonly Queue<BuildingPlacementDustParticle> roadEffectPool = new Queue<BuildingPlacementDustParticle>();
        private readonly Queue<Vector2Int> pendingRoadReveals = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedRoadReveals = new HashSet<Vector2Int>();

        private GridMapData mapData;
        private TileBase emptyTile;
        private TileBase roadTile;
        private TileBase ownedTile;
        private TileBase highlightTile;
        private TileBase waterTile;
        private TileBase waterFoamTile;
        private Material runtimeWaterFoamMaterial;
        private Texture2D runtimeWaterFoamTexture;
        private Sprite runtimeWaterFoamSprite;
        private TileBase[] grassVariantTiles = System.Array.Empty<TileBase>();
        private RoadVisualResolver roadVisualResolver;
        private Vector3Int? highlightedCell;
        private Vector3Int? buildingFadeCenterCell;
        private Vector2Int lastCameraChunk = new Vector2Int(int.MinValue, int.MinValue);
        private int buildingSortingLayerId;
        private int buildingSortingLayerOrder;
        private int effectiveDecorationSortingOrderBase;
        private int decorationSortingDepthOrigin;
        private Coroutine roadRevealCoroutine;
        private Coroutine purchasableHintCoroutine;
        private int createdRoadEffects;
        private Color highlightTilemapBaseColor = Color.white;

        private sealed class ActiveDecoration
        {
            public GameObject Instance;
            public SpriteRenderer Renderer;
            public int SortingOrderOffset;
        }

        public GridMapData MapData => mapData;
        public float TileWidth => tileWidth;
        public float TileHeight => tileHeight;
        public BuildingDefinition[] BuildingDefinitions => GetBuildingDefinitions();

        public event System.Action<BuildingInstance> BuildingQualityRevealed;

        public void Initialize(GridMapData data, Camera camera)
        {
            mapData = data;
            targetCamera = camera;
            mapData.SetProceduralRoadNetwork(proceduralRoadNetwork);
            mapData.ConfigureWorld(worldSettings);
            mapData.DevelopmentChanged -= OnDevelopmentChanged;
            mapData.DevelopmentChanged += OnDevelopmentChanged;
            mapData.RevealChanged -= OnRevealChanged;
            mapData.RevealChanged += OnRevealChanged;
            mapData.DecorationsCleared -= OnDecorationsCleared;
            mapData.DecorationsCleared += OnDecorationsCleared;
            mapData.CellChanged -= OnCellChanged;
            mapData.CellChanged += OnCellChanged;
            EnsureDefaultRoadVisualDefinitions();
            ConfigureTilemaps();
            if (highlightTilemap != null)
            {
                highlightTilemapBaseColor = highlightTilemap.color;
            }
            CreateTiles();
            roadVisualResolver = new RoadVisualResolver(roadVisualDefinitions);
            CreateRoadTiles();
            RefreshVisibleChunks(force: true);

            if (debugMap)
            {
                Debug.Log($"ChunkedGridMapRenderer initialized. chunkSize={mapData.ChunkSize}, activeChunks={activeChunks.Count}, tilemap={tilemap?.name}, highlightTilemap={highlightTilemap?.name}", this);
            }
        }

        private void OnDestroy()
        {
            if (mapData != null)
            {
                mapData.DevelopmentChanged -= OnDevelopmentChanged;
                mapData.RevealChanged -= OnRevealChanged;
                mapData.DecorationsCleared -= OnDecorationsCleared;
                mapData.CellChanged -= OnCellChanged;
            }

            if (runtimeWaterFoamMaterial != null)
            {
                Destroy(runtimeWaterFoamMaterial);
            }

            if (runtimeWaterFoamSprite != null)
            {
                Destroy(runtimeWaterFoamSprite);
            }

            if (runtimeWaterFoamTexture != null)
            {
                Destroy(runtimeWaterFoamTexture);
            }
        }

        private void LateUpdate()
        {
            if (mapData == null || targetCamera == null || tilemap == null)
            {
                return;
            }

            RefreshVisibleChunks(force: false);
        }

        private void RefreshVisibleChunks(bool force)
        {
            Vector2Int cameraChunk = WorldToChunk(targetCamera.transform.position);
            if (!force && cameraChunk == lastCameraChunk)
            {
                ProcessPendingChunkTransitions(forceAll: false);
                return;
            }

            lastCameraChunk = cameraChunk;
            decorationSortingDepthOrigin = (cameraChunk.x + cameraChunk.y) * mapData.ChunkSize;
            RebuildPendingChunkTransitions(cameraChunk);
            ProcessPendingChunkTransitions(force);

            // Existing renderers need rebasing only when the camera enters a new chunk.
            // Renderers loaded over subsequent frames use the current origin immediately.
            RefreshActiveBuildingSortingOrders();
            RefreshActiveDecorationSortingOrders();
        }

        private void RebuildPendingChunkTransitions(Vector2Int cameraChunk)
        {
            neededChunksBuffer.Clear();
            pendingChunkLoads.Clear();
            pendingChunkUnloads.Clear();

            for (int y = cameraChunk.y - visibleChunkRadius; y <= cameraChunk.y + visibleChunkRadius; y++)
            {
                for (int x = cameraChunk.x - visibleChunkRadius; x <= cameraChunk.x + visibleChunkRadius; x++)
                {
                    neededChunksBuffer.Add(new Vector2Int(x, y));
                }
            }

            foreach (Vector2Int chunk in activeChunks)
            {
                if (!neededChunksBuffer.Contains(chunk))
                {
                    pendingChunkUnloads.Enqueue(chunk);
                }
            }

            for (int y = cameraChunk.y - visibleChunkRadius; y <= cameraChunk.y + visibleChunkRadius; y++)
            {
                for (int x = cameraChunk.x - visibleChunkRadius; x <= cameraChunk.x + visibleChunkRadius; x++)
                {
                    Vector2Int chunk = new Vector2Int(x, y);
                    if (!activeChunks.Contains(chunk))
                    {
                        pendingChunkLoads.Enqueue(chunk);
                    }
                }
            }
        }

        private void ProcessPendingChunkTransitions(bool forceAll)
        {
            int transitionBudget = forceAll ? int.MaxValue : Mathf.Max(1, chunkTransitionsPerFrame);
            while (transitionBudget-- > 0
                   && (pendingChunkUnloads.Count > 0 || pendingChunkLoads.Count > 0))
            {
                // Return outgoing decorations to the pool before loading the incoming chunk.
                if (pendingChunkUnloads.Count > 0)
                {
                    Vector2Int chunk = pendingChunkUnloads.Dequeue();
                    if (activeChunks.Remove(chunk))
                    {
                        UnloadChunk(chunk);
                    }
                }

                if (pendingChunkLoads.Count > 0)
                {
                    Vector2Int chunk = pendingChunkLoads.Dequeue();
                    if (neededChunksBuffer.Contains(chunk) && activeChunks.Add(chunk))
                    {
                        LoadChunk(chunk);
                    }
                }
            }
        }

        private void ConfigureTilemaps()
        {
            if (grid != null)
            {
                grid.cellLayout = GridLayout.CellLayout.Isometric;
                grid.cellSize = new Vector3(tileWidth, tileHeight, 1f);
            }

            if (tilemap != null)
            {
                tilemap.ClearAllTiles();
                ConfigureRendererMode(tilemap, TilemapRenderer.Mode.Chunk);
            }

            EnsureWaterFoamTilemap();
            if (waterFoamTilemap != null)
            {
                waterFoamTilemap.ClearAllTiles();
                ConfigureRendererMode(waterFoamTilemap, TilemapRenderer.Mode.Chunk);
            }

            EnsureGroundDecorationTilemap();
            if (groundDecorationTilemap != null)
            {
                groundDecorationTilemap.ClearAllTiles();
                ConfigureRendererMode(groundDecorationTilemap, TilemapRenderer.Mode.Chunk);
            }

            if (buildingTilemap != null)
            {
                buildingTilemap.ClearAllTiles();
                TilemapRenderer buildingTilemapRenderer = buildingTilemap.GetComponent<TilemapRenderer>();
                if (buildingTilemapRenderer != null)
                {
                    buildingSortingLayerId = buildingTilemapRenderer.sortingLayerID;
                    buildingSortingLayerOrder = buildingTilemapRenderer.sortingOrder;
                    if (buildingMaterial == null)
                    {
                        buildingMaterial = buildingTilemapRenderer.sharedMaterial;
                    }
                }
            }

            CalculateDecorationSortingRange();

            if (highlightTilemap != null)
            {
                highlightTilemap.ClearAllTiles();
                ConfigureRendererMode(highlightTilemap, TilemapRenderer.Mode.Chunk);
            }

            activeChunks.Clear();
            neededChunksBuffer.Clear();
            pendingChunkLoads.Clear();
            pendingChunkUnloads.Clear();
            activeBuildingCells.Clear();
            ClearBuildingRenderers();
            ClearActiveDecorations();
            highlightedCell = null;
            buildingFadeCenterCell = null;
            lastCameraChunk = new Vector2Int(int.MinValue, int.MinValue);
            EnsureBuildingRoot();
            EnsureDecorationRoot();
            CacheDecorationDefinitions();
            PrewarmRoadEffectPool();
        }

        private void CreateTiles()
        {
            emptyTile = CreateTile(LoadSprite(emptySpritePath));
            roadTile = CreateTile(LoadSprite(roadSpritePath));
            ownedTile = CreateTile(LoadSprite(ownedSpritePath));
            highlightTile = CreateTile(LoadSprite(highlightSpritePath));
            waterTile = CreateTile(waterSprite != null ? waterSprite : LoadSprite(emptySpritePath), new Color(0.35f, 0.68f, 0.9f, 1f));
            Sprite foamMaskSprite = CreateWaterFoamMaskSprite();
            waterFoamTile = CreateTile(foamMaskSprite);
            ConfigureWaterFoamSpriteUv(foamMaskSprite);
            grassVariantTiles = new TileBase[grassVariantSprites == null ? 0 : grassVariantSprites.Length];
            for (int i = 0; i < grassVariantTiles.Length; i++)
            {
                grassVariantTiles[i] = CreateTile(grassVariantSprites[i]);
            }
            buildingTiles.Clear();
            buildingSprites.Clear();
            buildingSpritesByPath.Clear();
            buildingGroundTilesByPath.Clear();
            buildingDefinitionsById.Clear();
            groundDecorationTilesBySprite.Clear();

            BuildingDefinition[] definitions = GetBuildingDefinitions();
            foreach (BuildingDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.buildingId))
                {
                    continue;
                }

                buildingDefinitionsById[definition.buildingId] = definition;
                CacheBuildingSprites(definition);
                CacheBuildingGroundTile(definition);
                Sprite buildingSprite = GetCachedBuildingSprite(definition.DefaultSpritePath);
                buildingSprites[definition.buildingId] = buildingSprite;
                buildingTiles[definition.buildingId] = CreateTile(buildingSprite, new Color(1f, 1f, 1f, buildingAlpha));
            }
        }

        private void CacheBuildingSprites(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            CacheBuildingSpritePath(definition.spritePath);
            if (definition.spritePaths == null)
            {
                return;
            }

            for (int i = 0; i < definition.spritePaths.Length; i++)
            {
                CacheBuildingSpritePath(definition.spritePaths[i]);
            }
        }

        private void CacheBuildingSpritePath(string spritePath)
        {
            if (string.IsNullOrWhiteSpace(spritePath) || buildingSpritesByPath.ContainsKey(spritePath))
            {
                return;
            }

            buildingSpritesByPath[spritePath] = LoadSprite(spritePath);
        }

        private void CacheBuildingGroundTile(BuildingDefinition definition)
        {
            if (definition == null
                || !definition.replaceGroundTile
                || string.IsNullOrWhiteSpace(definition.groundSpritePath)
                || buildingGroundTilesByPath.ContainsKey(definition.groundSpritePath))
            {
                return;
            }

            Sprite sprite = LoadSprite(definition.groundSpritePath);
            if (sprite != null)
            {
                buildingGroundTilesByPath[definition.groundSpritePath] = CreateTile(sprite);
            }
        }

        private Sprite GetCachedBuildingSprite(string spritePath)
        {
            if (string.IsNullOrWhiteSpace(spritePath))
            {
                return null;
            }

            return buildingSpritesByPath.TryGetValue(spritePath, out Sprite sprite) ? sprite : null;
        }

        private void CreateRoadTiles()
        {
            roadTilesByMask.Clear();

            foreach (RoadVisualDefinition definition in roadVisualDefinitions)
            {
                if (definition == null)
                {
                    continue;
                }

                roadTilesByMask[definition.connectionMask & 15] = CreateRoadTile(definition);
            }
        }

        private TileBase CreateRoadTile(RoadVisualDefinition definition)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = GetRoadSprite(definition);
            tile.color = Color.white;
            tile.flags = TileFlags.None;
            tile.transform = definition.flipY180
                ? Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one)
                : Matrix4x4.identity;
            return tile;
        }

        private Sprite GetRoadSprite(RoadVisualDefinition definition)
        {
            if (definition.sprite != null)
            {
                return definition.sprite;
            }

            if (definition.prefab != null)
            {
                SpriteRenderer prefabRenderer = definition.prefab.GetComponentInChildren<SpriteRenderer>(includeInactive: true);
                if (prefabRenderer != null && prefabRenderer.sprite != null)
                {
                    return prefabRenderer.sprite;
                }
            }

            return LoadSprite(definition.spritePath);
        }

        private BuildingDefinition[] GetBuildingDefinitions()
        {
            if (buildingDatabase != null && buildingDatabase.buildings != null && buildingDatabase.buildings.Count > 0)
            {
                return buildingDatabase.buildings.ToArray();
            }

            return System.Array.Empty<BuildingDefinition>();
        }

        private void EnsureDefaultRoadVisualDefinitions()
        {
            if (roadVisualDefinitions != null && roadVisualDefinitions.Length > 0)
            {
                return;
            }

            roadVisualDefinitions = new[]
            {
                new RoadVisualDefinition { connectionMask = 5, spritePath = "Sprites/Load1" },
                new RoadVisualDefinition { connectionMask = 10, spritePath = "Sprites/Load1", flipY180 = true },
                new RoadVisualDefinition { connectionMask = 3, spritePath = "Sprites/Corner1" },
                new RoadVisualDefinition { connectionMask = 6, spritePath = "Sprites/Corner2" },
                new RoadVisualDefinition { connectionMask = 12, spritePath = "Sprites/Corner3" },
                new RoadVisualDefinition { connectionMask = 9, spritePath = "Sprites/Corner4" },
                new RoadVisualDefinition { connectionMask = 7, spritePath = "Sprites/LoadT2" },
                new RoadVisualDefinition { connectionMask = 11, spritePath = "Sprites/LoadT2", flipY180 = true },
                new RoadVisualDefinition { connectionMask = 13, spritePath = "Sprites/LoadT1" },
                new RoadVisualDefinition { connectionMask = 14, spritePath = "Sprites/LoadT1", flipY180 = true },
                new RoadVisualDefinition { connectionMask = 15, spritePath = "Sprites/LoadCross" }
            };
        }

        private TileBase CreateTile(Sprite sprite)
        {
            return CreateTile(sprite, Color.white);
        }

        private TileBase CreateTile(Sprite sprite, Color color)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            tile.flags = TileFlags.None;
            return tile;
        }

        private Sprite LoadSprite(string pathWithoutExtension)
        {
#if UNITY_EDITOR
            TryAssignNormalMap(pathWithoutExtension);
#endif

            Sprite sprite = Resources.Load<Sprite>(pathWithoutExtension);
            if (sprite != null)
            {
                return sprite;
            }

#if UNITY_EDITOR
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{pathWithoutExtension}.png");
            if (sprite != null)
            {
                return sprite;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath($"Assets/{pathWithoutExtension}.png");
            foreach (Object asset in assets)
            {
                if (asset is Sprite spriteAsset)
                {
                    return spriteAsset;
                }
            }
#endif

            Debug.LogError($"Tile sprite not found: {pathWithoutExtension}");
            return null;
        }

#if UNITY_EDITOR
        private void TryAssignNormalMap(string spritePathWithoutExtension)
        {
            string spriteAssetPath = $"Assets/{spritePathWithoutExtension}.png";
            string normalAssetPath = $"Assets/{spritePathWithoutExtension}_n.png";
            Texture2D normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalAssetPath);
            if (normalTexture == null)
            {
                return;
            }

            ConfigureNormalTextureImporter(normalAssetPath);

            TextureImporter importer = AssetImporter.GetAtPath(spriteAssetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            SecondarySpriteTexture[] secondaryTextures = importer.secondarySpriteTextures;
            for (int i = 0; i < secondaryTextures.Length; i++)
            {
                if (secondaryTextures[i].name == "_NormalMap")
                {
                    if (secondaryTextures[i].texture == normalTexture)
                    {
                        return;
                    }

                    secondaryTextures[i].texture = normalTexture;
                    importer.secondarySpriteTextures = secondaryTextures;
                    importer.SaveAndReimport();
                    return;
                }
            }

            List<SecondarySpriteTexture> updatedTextures = new List<SecondarySpriteTexture>(secondaryTextures.Length + 1);
            updatedTextures.AddRange(secondaryTextures);
            updatedTextures.Add(new SecondarySpriteTexture
            {
                name = "_NormalMap",
                texture = normalTexture
            });

            importer.secondarySpriteTextures = updatedTextures.ToArray();
            importer.SaveAndReimport();
        }

        private void ConfigureNormalTextureImporter(string normalAssetPath)
        {
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalAssetPath) as TextureImporter;
            if (normalImporter == null)
            {
                return;
            }

            bool changed = false;
            if (normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                changed = true;
            }

            if (normalImporter.sRGBTexture)
            {
                normalImporter.sRGBTexture = false;
                changed = true;
            }

            if (changed)
            {
                normalImporter.SaveAndReimport();
            }
        }
#endif

        private void LoadChunk(Vector2Int chunkPosition)
        {
            int startX = chunkPosition.x * mapData.ChunkSize;
            int startY = chunkPosition.y * mapData.ChunkSize;

            for (int y = 0; y < mapData.ChunkSize; y++)
            {
                for (int x = 0; x < mapData.ChunkSize; x++)
                {
                    GridCellData cell = mapData.GetCell(startX + x, startY + y);
                    Vector3Int tilePosition = new Vector3Int(cell.x, cell.y, 0);
                    if (mapData.IsRoadVisible(cell.x, cell.y))
                    {
                        ClearWaterFoam(tilePosition);
                        SetRoadTileAt(tilePosition, GetRoadConnectionMask(cell.x, cell.y));
                        SetBuildingTile(tilePosition, null);
                        RemoveDecoration(new Vector2Int(cell.x, cell.y));
                        continue;
                    }

                    SetGroundTileAt(tilePosition, cell);
                    SetBuildingTile(tilePosition, null);
                    RefreshBuildingRendererForCell(cell);
                    RefreshDecorationForCell(cell);
                }
            }
        }

        private void UnloadChunk(Vector2Int chunkPosition)
        {
            int startX = chunkPosition.x * mapData.ChunkSize;
            int startY = chunkPosition.y * mapData.ChunkSize;

            for (int y = 0; y < mapData.ChunkSize; y++)
            {
                for (int x = 0; x < mapData.ChunkSize; x++)
                {
                    Vector3Int tilePosition = new Vector3Int(startX + x, startY + y, 0);
                    tilemap.SetTile(tilePosition, null);
                    ClearWaterFoam(tilePosition);
                    SetBuildingTile(tilePosition, null);
                    RemoveDecoration(new Vector2Int(tilePosition.x, tilePosition.y));
                }
            }
        }

        private TileBase GetGroundTile(GridCellData cell)
        {
            if (cell.CellType == GridCellType.Water)
            {
                return waterTile;
            }

            if (mapData.IsRoadVisible(cell.x, cell.y))
            {
                return GetRoadTile(GetRoadConnectionMask(cell.x, cell.y));
            }

            // Undeveloped roads keep their logical road state, but visually use the
            // base Grass1 tile until the road is revealed.
            if (mapData.IsRoad(cell.x, cell.y))
            {
                return emptyTile;
            }

            if (mapData.TryGetBuildingInstanceAtCell(cell, out BuildingInstance instance)
                && instance != null
                && instance.Definition != null
                && instance.Definition.replaceGroundTile
                && !string.IsNullOrWhiteSpace(instance.Definition.groundSpritePath)
                && buildingGroundTilesByPath.TryGetValue(instance.Definition.groundSpritePath, out TileBase buildingGroundTile))
            {
                return buildingGroundTile;
            }

            if (cell.owned)
            {
                return ownedTile;
            }

            if (grassVariantTiles.Length > 0)
            {
                int index = mapData.GetVisualVariant(cell.x, cell.y, grassVariantTiles.Length, 211);
                if (grassVariantTiles[index] != null)
                {
                    return grassVariantTiles[index];
                }
            }

            return emptyTile;
        }

        private TileBase GetBuildingTile(GridCellData cell)
        {
            return null;
        }

        private int GetRoadConnectionMask(int x, int y)
        {
            int mask = 0;
            if (mapData.IsRoad(x, y + 1))
            {
                mask |= 1;
            }

            if (mapData.IsRoad(x + 1, y))
            {
                mask |= 2;
            }

            if (mapData.IsRoad(x, y - 1))
            {
                mask |= 4;
            }

            if (mapData.IsRoad(x - 1, y))
            {
                mask |= 8;
            }

            return mask;
        }

        private void LogRoadVisualResolution(int x, int y, int mask)
        {
            if (!debugMap)
            {
                return;
            }

            int resolvedMask = -1;
            string spritePath = "fallback";
            bool flipped = false;
            if (roadVisualResolver != null && roadVisualResolver.TryResolve(mask, out RoadVisualDefinition definition))
            {
                resolvedMask = definition.connectionMask & 15;
                spritePath = definition.spritePath;
                flipped = definition.flipY180;
            }

            Debug.Log(
                $"Road reveal ({x},{y}) rawMask={mask} resolvedMask={resolvedMask} " +
                $"N={mapData.IsRoad(x, y + 1)} E={mapData.IsRoad(x + 1, y)} " +
                $"S={mapData.IsRoad(x, y - 1)} W={mapData.IsRoad(x - 1, y)} " +
                $"sprite={spritePath} flipY180={flipped}",
                this);
        }

        private TileBase GetRoadTile(int connectionMask)
        {
            if (roadVisualResolver == null || !roadVisualResolver.TryResolve(connectionMask, out RoadVisualDefinition visualDefinition))
            {
                return roadTile;
            }

            int resolvedMask = visualDefinition.connectionMask & 15;
            if (roadTilesByMask.TryGetValue(resolvedMask, out TileBase tile))
            {
                return tile;
            }

            return roadTile;
        }

        public bool TryGetVisibleCellAtWorld(Vector3 worldPosition, out GridCellData cell)
        {
            if (tilemap == null || mapData == null)
            {
                cell = null;
                return false;
            }

            Vector3Int tilePosition = tilemap.WorldToCell(worldPosition);
            if (!tilemap.HasTile(tilePosition))
            {
                if (debugMap)
                {
                    Debug.Log($"No tile at world={worldPosition}, cell={tilePosition}", this);
                }

                cell = null;
                return false;
            }

            cell = mapData.GetCell(tilePosition.x, tilePosition.y);
            if (debugMap)
            {
                Debug.Log($"Tile hit. world={worldPosition}, cell={tilePosition}, type={cell.type}, owned={cell.owned}", this);
            }

            return true;
        }

        public void HighlightCell(GridCellData cell)
        {
            ClearHighlight();

            if (highlightTilemap == null)
            {
                return;
            }

            highlightedCell = new Vector3Int(cell.x, cell.y, 0);
            highlightTilemap.SetTile(highlightedCell.Value, highlightTile);
            FadeBuildingsAroundCell(cell);
        }

        public void HighlightFootprint(GridCellData originCell, BuildingDefinition definition, bool valid)
        {
            ClearHighlight();

            if (highlightTilemap == null || originCell == null || definition == null)
            {
                return;
            }

            int width = Mathf.Max(1, definition.footprintWidth);
            int height = Mathf.Max(1, definition.footprintHeight);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    highlightTilemap.SetTile(new Vector3Int(originCell.x + x, originCell.y + y, 0), highlightTile);
                }
            }

            highlightedCell = new Vector3Int(originCell.x, originCell.y, 0);
            FadeBuildingsAroundCell(originCell);
        }

        public void SetCellOwned(GridCellData cell, bool owned)
        {
            if (owned)
            {
                if (!mapData.PurchaseLand(cell))
                {
                    return;
                }
            }
            else
            {
                mapData.SetOwned(cell.x, cell.y, false);
            }

            RemoveDecoration(new Vector2Int(cell.x, cell.y));
            RefreshVisibleCell(cell);
        }

        public bool CanPurchaseLand(GridCellData cell)
        {
            return mapData != null && mapData.CanPurchaseLand(cell);
        }

        public bool IsCellRevealed(GridCellData cell)
        {
            return mapData != null && cell != null && mapData.IsRevealed(cell.x, cell.y);
        }

        public void SetCellBuilding(GridCellData cell, string buildingId)
        {
            mapData.SetBuilding(cell.x, cell.y, buildingId);
            RefreshVisibleCell(cell);
        }

        public bool CanPlaceBuilding(BuildingDefinition definition, GridCellData originCell)
        {
            return mapData != null && mapData.CanPlaceBuilding(definition, originCell);
        }

        public BuildingInstance PlaceBuilding(GridCellData originCell, BuildingDefinition definition)
        {
            if (mapData == null || !mapData.CanPlaceBuilding(definition, originCell))
            {
                return null;
            }

            BuildingQualityRoll qualityRoll = definition.IsRoad
                ? BuildingQualitySettings.CreateNormalRoll()
                : buildingQualitySettings != null
                    ? buildingQualitySettings.Roll()
                    : BuildingQualitySettings.CreateNormalRoll();
            BuildingInstance instance = mapData.PlaceBuildingInstance(definition, originCell, qualityRoll);
            if (instance == null)
            {
                return null;
            }

            if (!definition.IsRoad)
            {
                RefreshBuildingInstance(instance);
            }
            IReadOnlyList<Vector2Int> occupiedCells = instance.OccupiedCells;
            for (int i = 0; i < occupiedCells.Count; i++)
            {
                RemoveDecoration(occupiedCells[i]);
            }
            if (!definition.IsRoad)
            {
                PlayBuildingPlacementFeedback(instance);
                BuildingQualityRevealed?.Invoke(instance);
            }
            return instance;
        }

        public bool IsRoad(GridCellData cell)
        {
            return mapData != null && cell != null && mapData.IsRoad(cell.x, cell.y);
        }

        public bool IsRoadVisible(GridCellData cell)
        {
            return mapData != null && cell != null && mapData.IsRoadVisible(cell.x, cell.y);
        }

        public bool CanRemoveRoad(GridCellData cell)
        {
            return mapData != null && mapData.CanRemoveRoad(cell);
        }

        public bool RemoveRoad(GridCellData cell)
        {
            return mapData != null && mapData.RemoveRoad(cell);
        }

        public bool CanRemovePlacedObject(GridCellData cell)
        {
            if (mapData == null || cell == null)
            {
                return false;
            }

            return mapData.CanRemoveRoad(cell)
                || mapData.TryGetBuildingInstanceAtCell(cell, out BuildingInstance instance)
                && instance != null;
        }

        public bool RemovePlacedObject(GridCellData cell)
        {
            if (!CanRemovePlacedObject(cell))
            {
                return false;
            }

            if (mapData.CanRemoveRoad(cell))
            {
                return mapData.RemoveRoad(cell);
            }

            if (!mapData.TryGetBuildingInstanceAtCell(cell, out BuildingInstance instance)
                || instance == null)
            {
                return false;
            }

            mapData.RemoveBuildingInstance(instance.InstanceId);
            return true;
        }

        public long GetDemolitionRefund(GridCellData cell)
        {
            if (mapData == null || cell == null
                || !mapData.TryGetBuildingInstanceAtCell(cell, out BuildingInstance instance)
                || instance?.Definition == null)
            {
                return 0L;
            }

            return LoadGame.Balance.EconomyRules.DemolitionRefund(instance.Definition.price);
        }

        public bool CanRemovePlayerRoad(GridCellData cell)
        {
            return CanRemoveRoad(cell);
        }

        public bool RemovePlayerRoad(GridCellData cell)
        {
            return RemoveRoad(cell);
        }

        private void PlayBuildingPlacementFeedback(BuildingInstance instance)
        {
            if (buildingPlacementFeedback == null || instance == null)
            {
                return;
            }

            Vector3Int originPosition = new Vector3Int(instance.OriginCell.x, instance.OriginCell.y, 0);
            if (!activeBuildingRenderers.TryGetValue(originPosition, out SpriteRenderer spriteRenderer)
                || spriteRenderer == null)
            {
                return;
            }

            buildingPlacementFeedback.Play(spriteRenderer.transform, spriteRenderer.transform.position);
        }

        public bool TryGetBuildingInstanceAtCell(GridCellData cell, out BuildingInstance instance)
        {
            if (mapData == null)
            {
                instance = null;
                return false;
            }

            return mapData.TryGetBuildingInstanceAtCell(cell, out instance);
        }

        public bool TryGetVisibleBuildingWorldPosition(BuildingInstance instance, out Vector3 worldPosition)
        {
            if (instance == null)
            {
                worldPosition = default;
                return false;
            }

            Vector3Int originPosition = new Vector3Int(instance.OriginCell.x, instance.OriginCell.y, 0);
            if (!activeBuildingRenderers.TryGetValue(originPosition, out SpriteRenderer spriteRenderer)
                || spriteRenderer == null
                || !spriteRenderer.gameObject.activeInHierarchy)
            {
                worldPosition = default;
                return false;
            }

            Bounds bounds = spriteRenderer.bounds;
            worldPosition = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            return true;
        }

        public bool TryGetBuildingDefinition(string buildingId, out BuildingDefinition definition)
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                definition = null;
                return false;
            }

            return buildingDefinitionsById.TryGetValue(buildingId, out definition);
        }

        public Sprite LoadBuildingIcon(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            return definition.icon != null ? definition.icon : GetCachedBuildingSprite(definition.DefaultSpritePath);
        }

        private void RefreshVisibleCell(GridCellData cell)
        {
            if (tilemap == null)
            {
                return;
            }

            Vector3Int tilePosition = new Vector3Int(cell.x, cell.y, 0);
            if (tilemap.HasTile(tilePosition))
            {
                SetGroundTileAt(tilePosition, cell);
                SetBuildingTile(tilePosition, null);
                RefreshBuildingRendererForCell(cell);
                RefreshDecorationForCell(cell);
            }
        }

        private void SetBuildingTile(Vector3Int tilePosition, TileBase tile)
        {
            if (tile != null)
            {
                activeBuildingCells.Add(tilePosition);
                SetBuildingRenderer(tilePosition);
                return;
            }

            activeBuildingCells.Remove(tilePosition);
            RemoveBuildingRenderer(tilePosition);

            if (buildingTilemap != null)
            {
                if (buildingTilemap.HasTile(tilePosition))
                {
                    buildingTilemap.SetTile(tilePosition, null);
                }
            }
        }

        private void SetBuildingRenderer(Vector3Int tilePosition)
        {
            if (mapData == null || tilemap == null)
            {
                return;
            }

            GridCellData cell = mapData.GetCell(tilePosition.x, tilePosition.y);
            if (cell == null || !mapData.TryGetBuildingInstanceAtCell(cell, out BuildingInstance instance) || instance == null || instance.Definition == null)
            {
                RemoveBuildingRenderer(tilePosition);
                return;
            }

            Vector3Int originPosition = new Vector3Int(instance.OriginCell.x, instance.OriginCell.y, 0);
            if (tilePosition != originPosition)
            {
                return;
            }

            BuildingDefinition definition = instance.Definition;
            Sprite sprite = GetCachedBuildingSprite(instance.VisualSpritePath);
            if (sprite == null && !buildingSprites.TryGetValue(definition.buildingId, out sprite))
            {
                RemoveBuildingRenderer(tilePosition);
                return;
            }

            if (!activeBuildingRenderers.TryGetValue(tilePosition, out SpriteRenderer spriteRenderer) || spriteRenderer == null)
            {
                spriteRenderer = CreateBuildingRenderer(tilePosition, definition);
                activeBuildingRenderers[tilePosition] = spriteRenderer;
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.transform.position = GetBuildingWorldPosition(instance) + buildingWorldOffset + definition.visualOffset;
            spriteRenderer.transform.localScale = GetBuildingVisualScale(definition);
            spriteRenderer.sortingOrder = GetBuildingSortingOrder(tilePosition);
            ApplyBuildingQuality(spriteRenderer, instance.Quality);
            ApplyBuildingRendererAlpha(spriteRenderer, GetBuildingAlpha(tilePosition));
        }

        private static Vector3 GetBuildingVisualScale(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return Vector3.one;
            }

            float footprintScale = Mathf.Max(
                1,
                Mathf.Max(definition.footprintWidth, definition.footprintHeight));
            return Vector3.one * footprintScale;
        }

        public Vector3 GetCellCenterWorld(int x, int y)
        {
            return tilemap == null ? Vector3.zero : tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
        }

        public void ShowPurchasableLandHint()
        {
            if (highlightTilemap == null || mapData == null)
            {
                return;
            }

            if (purchasableHintCoroutine != null)
            {
                StopCoroutine(purchasableHintCoroutine);
            }

            ClearHighlightImmediate();
            foreach (Vector2Int chunkPosition in activeChunks)
            {
                int startX = chunkPosition.x * mapData.ChunkSize;
                int startY = chunkPosition.y * mapData.ChunkSize;
                for (int y = 0; y < mapData.ChunkSize; y++)
                {
                    for (int x = 0; x < mapData.ChunkSize; x++)
                    {
                        int cellX = startX + x;
                        int cellY = startY + y;
                        Vector3Int tilePosition = new Vector3Int(cellX, cellY, 0);
                        if (!tilemap.HasTile(tilePosition))
                        {
                            continue;
                        }

                        GridCellData cell = mapData.GetCell(cellX, cellY);
                        if (mapData.CanPurchaseLand(cell))
                        {
                            highlightTilemap.SetTile(tilePosition, highlightTile);
                        }
                    }
                }
            }

            purchasableHintCoroutine = StartCoroutine(AnimatePurchasableLandHint());
        }

        private IEnumerator AnimatePurchasableLandHint()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, purchasableHintDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                Color color = purchasableHintColor;
                color.a *= 1f - Mathf.SmoothStep(0f, 1f, normalizedTime);
                highlightTilemap.color = color;
                yield return null;
            }

            purchasableHintCoroutine = null;
            ClearHighlightImmediate();
        }

        private static void ApplyBuildingQuality(SpriteRenderer spriteRenderer, BuildingQuality quality)
        {
            BuildingQualityVisual qualityVisual = spriteRenderer.GetComponent<BuildingQualityVisual>();
            if (qualityVisual == null)
            {
                qualityVisual = spriteRenderer.gameObject.AddComponent<BuildingQualityVisual>();
            }

            qualityVisual.Initialize(spriteRenderer);
            qualityVisual.ApplyQuality(quality);
        }

        private void RefreshBuildingRendererForCell(GridCellData cell)
        {
            if (cell == null || mapData == null || !mapData.TryGetBuildingInstanceAtCell(cell, out BuildingInstance instance) || instance == null)
            {
                return;
            }

            if (instance.Definition != null && instance.Definition.IsRoad)
            {
                return;
            }

            RefreshBuildingInstance(instance);
        }

        private void RefreshBuildingInstance(BuildingInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            SetBuildingRenderer(new Vector3Int(instance.OriginCell.x, instance.OriginCell.y, 0));
        }

        private Vector3 GetBuildingWorldPosition(BuildingInstance instance)
        {
            int width = Mathf.Max(1, instance.Definition.footprintWidth);
            int height = Mathf.Max(1, instance.Definition.footprintHeight);
            Vector3 total = Vector3.zero;
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    total += tilemap.GetCellCenterWorld(new Vector3Int(instance.OriginCell.x + x, instance.OriginCell.y + y, 0));
                    count++;
                }
            }

            return count <= 0 ? tilemap.GetCellCenterWorld(new Vector3Int(instance.OriginCell.x, instance.OriginCell.y, 0)) : total / count;
        }

        private SpriteRenderer CreateBuildingRenderer(Vector3Int tilePosition, BuildingDefinition definition)
        {
            EnsureBuildingRoot();

            GameObject buildingObject = definition != null && definition.prefab != null
                ? Instantiate(definition.prefab)
                : new GameObject($"Building {tilePosition.x},{tilePosition.y}");

            buildingObject.name = $"Building {tilePosition.x},{tilePosition.y}";
            buildingObject.transform.SetParent(buildingRoot, worldPositionStays: false);
            SpriteRenderer spriteRenderer = buildingObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = buildingObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sortingLayerID = buildingSortingLayerId;
            if (buildingMaterial != null)
            {
                spriteRenderer.sharedMaterial = buildingMaterial;
            }

            spriteRenderer.sortingOrder = GetBuildingSortingOrder(tilePosition);
            return spriteRenderer;
        }

        private void RemoveBuildingRenderer(Vector3Int tilePosition)
        {
            if (!activeBuildingRenderers.TryGetValue(tilePosition, out SpriteRenderer spriteRenderer))
            {
                return;
            }

            activeBuildingRenderers.Remove(tilePosition);
            if (spriteRenderer != null)
            {
                Destroy(spriteRenderer.gameObject);
            }
        }

        private void ClearBuildingRenderers()
        {
            foreach (KeyValuePair<Vector3Int, SpriteRenderer> entry in activeBuildingRenderers)
            {
                if (entry.Value != null)
                {
                    Destroy(entry.Value.gameObject);
                }
            }

            activeBuildingRenderers.Clear();
        }

        private void EnsureBuildingRoot()
        {
            if (buildingRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("Building Renderers");
            rootObject.transform.SetParent(transform, worldPositionStays: false);
            buildingRoot = rootObject.transform;
        }

        private int GetBuildingSortingOrder(Vector3Int tilePosition)
        {
            return GetWorldObjectSortingOrder(tilePosition.x, tilePosition.y, buildingSortingOrderOffset);
        }

        private void RefreshActiveBuildingSortingOrders()
        {
            foreach (KeyValuePair<Vector3Int, SpriteRenderer> pair in activeBuildingRenderers)
            {
                if (pair.Value != null)
                {
                    pair.Value.sortingOrder = GetBuildingSortingOrder(pair.Key);
                }
            }

        }

        public void FadeBuildingsAroundCell(GridCellData cell)
        {
            if (!fadeBuildingsAroundSelectedCell || cell == null)
            {
                return;
            }

            buildingFadeCenterCell = new Vector3Int(cell.x, cell.y, 0);
            RefreshBuildingAlphas();
        }

        public void ClearBuildingFade()
        {
            buildingFadeCenterCell = null;
            RefreshBuildingAlphas();
        }

        private void RefreshBuildingAlphas()
        {
            foreach (KeyValuePair<Vector3Int, SpriteRenderer> entry in activeBuildingRenderers)
            {
                ApplyBuildingRendererAlpha(entry.Value, GetBuildingAlpha(entry.Key));
            }
        }

        private float GetBuildingAlpha(Vector3Int buildingCell)
        {
            if (!buildingFadeCenterCell.HasValue)
            {
                return buildingAlpha;
            }

            Vector3Int centerCell = buildingFadeCenterCell.Value;
            int distanceX = Mathf.Abs(buildingCell.x - centerCell.x);
            int distanceY = Mathf.Abs(buildingCell.y - centerCell.y);
            int distance = Mathf.Max(distanceX, distanceY);
            return distance > 0 && distance <= selectedCellFadeRadius ? selectedAreaBuildingAlpha : buildingAlpha;
        }

        private static void ApplyBuildingRendererAlpha(SpriteRenderer spriteRenderer, float alpha)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private void OnDevelopmentChanged(IReadOnlyList<Vector2Int> changedCells)
        {
            if (tilemap == null || changedCells == null)
            {
                return;
            }

            List<Vector2Int> visibleRoads = new List<Vector2Int>();
            for (int i = 0; i < changedCells.Count; i++)
            {
                Vector2Int position = changedCells[i];
                Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
                if (!tilemap.HasTile(tilePosition))
                {
                    continue;
                }

                GridCellData cell = mapData.GetCell(position.x, position.y);
                if (mapData.IsRoadVisible(cell.x, cell.y))
                {
                    RemoveDecoration(position);
                    SetTileAt(tilePosition, emptyTile, Matrix4x4.identity);
                    visibleRoads.Add(position);
                }
                else
                {
                    SetGroundTileAt(tilePosition, cell);
                }
            }

            QueueRoadReveals(visibleRoads);
        }

        private void OnRevealChanged(IReadOnlyList<Vector2Int> changedCells)
        {
            if (tilemap == null || changedCells == null || changedCells.Count == 0)
            {
                return;
            }

            List<Vector2Int> visibleRoads = new List<Vector2Int>();
            for (int i = 0; i < changedCells.Count; i++)
            {
                Vector2Int position = changedCells[i];
                Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
                if (!tilemap.HasTile(tilePosition))
                {
                    continue;
                }

                GridCellData cell = mapData.GetCell(position.x, position.y);
                if (mapData.IsRoadVisible(cell.x, cell.y))
                {
                    SetTileAt(tilePosition, emptyTile, Matrix4x4.identity);
                    visibleRoads.Add(position);
                }
                else
                {
                    SetGroundTileAt(tilePosition, cell);
                }

                RefreshDecorationForCell(cell);
            }

            QueueRoadReveals(visibleRoads);
        }

        private void QueueRoadReveals(IReadOnlyList<Vector2Int> roadPositions)
        {
            if (roadPositions == null)
            {
                return;
            }

            for (int i = 0; i < roadPositions.Count; i++)
            {
                Vector2Int position = roadPositions[i];
                if (queuedRoadReveals.Add(position))
                {
                    pendingRoadReveals.Enqueue(position);
                }
            }

            if (roadRevealCoroutine == null && pendingRoadReveals.Count > 0)
            {
                roadRevealCoroutine = StartCoroutine(RevealQueuedRoads());
            }
        }

        private IEnumerator RevealQueuedRoads()
        {
            float delay = worldSettings == null ? 0f : worldSettings.roadRevealStepDelay;
            bool animate = worldSettings != null && worldSettings.animateRoadReveal && delay > 0f;
            WaitForSeconds stepWait = animate ? new WaitForSeconds(delay) : null;
            int processedThisFrame = 0;
            int batchSize = Mathf.Max(1, roadRevealBatchSize);
            while (pendingRoadReveals.Count > 0)
            {
                Vector2Int position = pendingRoadReveals.Dequeue();
                queuedRoadReveals.Remove(position);
                Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
                if (tilemap.HasTile(tilePosition) && mapData.IsRoadVisible(position.x, position.y))
                {
                    int connectionMask = GetRoadConnectionMask(position.x, position.y);
                    LogRoadVisualResolution(position.x, position.y, connectionMask);
                    SetRoadTileAt(tilePosition, connectionMask);
                    RefreshVisibleRoadNeighbors(position.x, position.y);
                    PlayRoadRevealEffect(tilemap.GetCellCenterWorld(tilePosition));
                }
                else if (tilemap.HasTile(tilePosition))
                {
                    SetGroundTileAt(tilePosition, mapData.GetCell(position.x, position.y));
                }

                if (animate)
                {
                    yield return stepWait;
                }
                else if (++processedThisFrame >= batchSize)
                {
                    processedThisFrame = 0;
                    yield return null;
                }
            }

            roadRevealCoroutine = null;
        }

        private void RefreshVisibleRoadNeighbors(int x, int y)
        {
            RefreshVisibleRoad(x, y + 1);
            RefreshVisibleRoad(x + 1, y);
            RefreshVisibleRoad(x, y - 1);
            RefreshVisibleRoad(x - 1, y);
        }

        private void RefreshVisibleRoad(int x, int y)
        {
            if (!mapData.IsRoadVisible(x, y))
            {
                return;
            }

            Vector3Int position = new Vector3Int(x, y, 0);
            if (tilemap.HasTile(position))
            {
                SetRoadTileAt(position, GetRoadConnectionMask(x, y));
            }
        }

        private void OnDecorationsCleared(IReadOnlyList<Vector2Int> changedCells)
        {
            if (changedCells == null)
            {
                return;
            }

            for (int i = 0; i < changedCells.Count; i++)
            {
                RemoveDecoration(changedCells[i]);
            }
        }

        private void OnCellChanged(GridCellData cell)
        {
            if (cell != null)
            {
                RefreshVisibleCell(cell);
            }
        }

        private void SetGroundTileAt(Vector3Int position, GridCellData cell)
        {
            if (mapData.IsRoadVisible(cell.x, cell.y))
            {
                ClearWaterFoam(position);
                SetRoadTileAt(position, GetRoadConnectionMask(cell.x, cell.y));
                return;
            }

            SetTileAt(position, GetGroundTile(cell), Matrix4x4.identity);
            SetWaterFoamAt(position, cell);
            if (IsGrassVisual(cell))
            {
                tilemap.SetColor(position, worldSettings.GetGrassTint(cell.x, cell.y));
            }
        }

        private bool IsGrassVisual(GridCellData cell)
        {
            if (cell == null || cell.owned || cell.CellType == GridCellType.Water)
            {
                return false;
            }

            return !mapData.IsRoadVisible(cell.x, cell.y);
        }

        private void SetRoadTileAt(Vector3Int position, int connectionMask)
        {
            ClearWaterFoam(position);
            TileBase roadVisual = GetRoadTile(connectionMask);
            Matrix4x4 transformMatrix = Matrix4x4.identity;
            if (roadVisualResolver != null
                && roadVisualResolver.TryResolve(connectionMask, out RoadVisualDefinition definition)
                && definition.flipY180)
            {
                transformMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            }

            SetTileAt(position, roadVisual, transformMatrix);
        }

        private void SetTileAt(Vector3Int position, TileBase tile, Matrix4x4 transformMatrix)
        {
            tilemap.SetTile(position, tile);
            tilemap.SetTransformMatrix(position, transformMatrix);
            tilemap.SetColor(position, Color.white);
        }

        private void CacheDecorationDefinitions()
        {
            decorationDefinitionsByType.Clear();
            if (decorationDefinitions == null)
            {
                return;
            }

            for (int i = 0; i < decorationDefinitions.Length; i++)
            {
                WorldDecorationDefinition definition = decorationDefinitions[i];
                if (definition != null)
                {
                    decorationDefinitionsByType[definition.type] = definition;
                }
            }
        }

        private void RefreshDecorationForCell(GridCellData cell)
        {
            Vector2Int position = new Vector2Int(cell.x, cell.y);
            RemoveDecoration(position);
            if (!mapData.TryGetDecorationType(cell.x, cell.y, out WorldDecorationType type)
                || !decorationDefinitionsByType.TryGetValue(type, out WorldDecorationDefinition definition)
                || definition.sprites == null
                || definition.sprites.Length == 0)
            {
                return;
            }

            int variant = mapData.GetVisualVariant(cell.x, cell.y, definition.sprites.Length, 307 + (int)type);
            Sprite sprite = definition.sprites[variant];
            if (sprite == null)
            {
                return;
            }

            Vector3Int tilePosition = new Vector3Int(cell.x, cell.y, 0);
            if (IsGroundDecoration(type))
            {
                if (groundDecorationTilemap == null)
                {
                    return;
                }

                TileBase decorationTile = GetGroundDecorationTile(sprite);
                groundDecorationTilemap.SetTile(tilePosition, decorationTile);
                groundDecorationTilemap.SetColor(tilePosition, Color.white);
                groundDecorationTilemap.SetTransformMatrix(
                    tilePosition,
                    Matrix4x4.TRS(definition.worldOffset, Quaternion.identity, Vector3.one));
                return;
            }

            ActiveDecoration active = AcquireDecoration();
            GameObject instance = active.Instance;
            SpriteRenderer spriteRenderer = active.Renderer;
            instance.transform.position = tilemap.GetCellCenterWorld(tilePosition) + definition.worldOffset;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.flipX = false;
            spriteRenderer.flipY = false;
            instance.SetActive(true);

            spriteRenderer.sortingOrder = GetDecorationSortingOrder(cell.x, cell.y, definition.sortingOrderOffset);
            active.SortingOrderOffset = definition.sortingOrderOffset;
            activeDecorations[position] = active;
        }

        private ActiveDecoration AcquireDecoration()
        {
            if (decorationPool.Count > 0)
            {
                return decorationPool.Dequeue();
            }

            EnsureDecorationRoot();
            GameObject instance = new GameObject("World Decoration");
            instance.transform.SetParent(decorationRoot, worldPositionStays: false);
            SpriteRenderer spriteRenderer = instance.AddComponent<SpriteRenderer>();
            TilemapRenderer groundRenderer = tilemap == null ? null : tilemap.GetComponent<TilemapRenderer>();
            if (groundRenderer != null)
            {
                spriteRenderer.sortingLayerID = buildingSortingLayerId;
                spriteRenderer.sharedMaterial = treeWindMaterial != null
                    ? treeWindMaterial
                    : groundRenderer.sharedMaterial;
            }

            instance.SetActive(false);
            return new ActiveDecoration
            {
                Instance = instance,
                Renderer = spriteRenderer
            };
        }

        private void RemoveDecoration(Vector2Int position)
        {
            Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
            if (groundDecorationTilemap != null && groundDecorationTilemap.HasTile(tilePosition))
            {
                groundDecorationTilemap.SetTile(tilePosition, null);
            }

            if (!activeDecorations.TryGetValue(position, out ActiveDecoration active))
            {
                return;
            }

            activeDecorations.Remove(position);
            if (active.Instance == null)
            {
                return;
            }

            if (active.Renderer != null)
            {
                active.Renderer.sprite = null;
                active.Renderer.color = Color.white;
                active.Renderer.flipX = false;
                active.Renderer.flipY = false;
            }

            active.Instance.SetActive(false);
            active.SortingOrderOffset = 0;
            decorationPool.Enqueue(active);

        }

        private void ClearActiveDecorations()
        {
            if (groundDecorationTilemap != null)
            {
                groundDecorationTilemap.ClearAllTiles();
            }

            if (activeDecorations.Count == 0)
            {
                return;
            }

            List<Vector2Int> positions = new List<Vector2Int>(activeDecorations.Keys);
            for (int i = 0; i < positions.Count; i++)
            {
                RemoveDecoration(positions[i]);
            }

        }

        private void EnsureDecorationRoot()
        {
            if (decorationRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("World Decorations");
            rootObject.transform.SetParent(transform, worldPositionStays: false);
            decorationRoot = rootObject.transform;
        }

        private void CalculateDecorationSortingRange()
        {
            effectiveDecorationSortingOrderBase = Mathf.Max(
                decorationSortingOrderBase,
                buildingSortingLayerOrder + buildingSortingOrderBase);
            if (tilemap == null || mapData == null)
            {
                return;
            }

            TilemapRenderer groundRenderer = tilemap.GetComponent<TilemapRenderer>();
            int groundSortingOrder = groundRenderer == null ? 0 : groundRenderer.sortingOrder;
            int chunkSize = Mathf.Max(1, mapData.ChunkSize);
            int radius = Mathf.Max(0, visibleChunkRadius);
            int maximumVisibleRelativeDepth = radius * chunkSize * 2 + (chunkSize - 1) * 2;
            int minimumBase = groundSortingOrder + 1
                + maximumVisibleRelativeDepth * Mathf.Max(0, buildingSortingOrderStep);
            effectiveDecorationSortingOrderBase = Mathf.Max(effectiveDecorationSortingOrderBase, minimumBase);

            if (debugMap && effectiveDecorationSortingOrderBase != decorationSortingOrderBase)
            {
                Debug.Log(
                    $"World object sorting base adjusted to {effectiveDecorationSortingOrderBase} " +
                    $"for visible relative depth {maximumVisibleRelativeDepth}.",
                    this);
            }
        }

        private int GetDecorationSortingOrder(int x, int y, int sortingOrderOffset)
        {
            return GetWorldObjectSortingOrder(x, y, sortingOrderOffset);
        }

        private int GetWorldObjectSortingOrder(int x, int y, int sortingOrderOffset)
        {
            int relativeDepth = x + y - decorationSortingDepthOrigin;
            return effectiveDecorationSortingOrderBase
                - relativeDepth * buildingSortingOrderStep
                + sortingOrderOffset;
        }

        private void RefreshActiveDecorationSortingOrders()
        {
            foreach (KeyValuePair<Vector2Int, ActiveDecoration> pair in activeDecorations)
            {
                ActiveDecoration active = pair.Value;
                if (active != null && active.Renderer != null)
                {
                    active.Renderer.sortingOrder = GetDecorationSortingOrder(
                        pair.Key.x,
                        pair.Key.y,
                        active.SortingOrderOffset);
                }
            }
        }

        private static bool IsGroundDecoration(WorldDecorationType type)
        {
            return type == WorldDecorationType.Flower
                || type == WorldDecorationType.Bush
                || type == WorldDecorationType.Rock;
        }

        private TileBase GetGroundDecorationTile(Sprite sprite)
        {
            if (groundDecorationTilesBySprite.TryGetValue(sprite, out TileBase tile))
            {
                return tile;
            }

            tile = CreateTile(sprite);
            groundDecorationTilesBySprite[sprite] = tile;
            return tile;
        }

        private void SetWaterFoamAt(Vector3Int position, GridCellData cell)
        {
            if (waterFoamTilemap == null || waterFoamTile == null || cell.CellType != GridCellType.Water)
            {
                ClearWaterFoam(position);
                return;
            }

            int shorelineMask = GetShorelineMask(cell.x, cell.y);
            waterFoamTilemap.SetTile(position, waterFoamTile);
            // Keep each direction in an independent vertex-color channel.
            // This avoids bit decoding errors after Tilemap color packing.
            waterFoamTilemap.SetColor(
                position,
                new Color(
                    (shorelineMask & 1) != 0 ? 0.75f : 0.25f,
                    (shorelineMask & 2) != 0 ? 0.75f : 0.25f,
                    (shorelineMask & 4) != 0 ? 0.75f : 0.25f,
                    (shorelineMask & 8) != 0 ? 0.75f : 0.25f));
            waterFoamTilemap.SetTransformMatrix(position, Matrix4x4.identity);
        }

        private int GetShorelineMask(int x, int y)
        {
            int mask = 0;
            if (!IsWaterCell(x, y + 1))
            {
                mask |= 1;
            }

            if (!IsWaterCell(x + 1, y))
            {
                mask |= 2;
            }

            if (!IsWaterCell(x, y - 1))
            {
                mask |= 4;
            }

            if (!IsWaterCell(x - 1, y))
            {
                mask |= 8;
            }

            return mask;
        }

        private bool IsWaterCell(int x, int y)
        {
            return mapData.GetCell(x, y).CellType == GridCellType.Water;
        }

        private void ClearWaterFoam(Vector3Int position)
        {
            if (waterFoamTilemap != null && waterFoamTilemap.HasTile(position))
            {
                waterFoamTilemap.SetTile(position, null);
            }
        }

        private void EnsureWaterFoamTilemap()
        {
            if (grid == null)
            {
                return;
            }

            GameObject tilemapObject;
            if (waterFoamTilemap != null)
            {
                tilemapObject = waterFoamTilemap.gameObject;
            }
            else
            {
                Transform existing = grid.transform.Find("Water Shoreline Foam");
                if (existing != null)
                {
                    tilemapObject = existing.gameObject;
                    waterFoamTilemap = tilemapObject.GetComponent<Tilemap>();
                }
                else
                {
                    tilemapObject = new GameObject("Water Shoreline Foam");
                    tilemapObject.transform.SetParent(grid.transform, worldPositionStays: false);
                    waterFoamTilemap = tilemapObject.AddComponent<Tilemap>();
                }
            }

            if (waterFoamTilemap == null)
            {
                waterFoamTilemap = tilemapObject.AddComponent<Tilemap>();
            }

            TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = tilemapObject.AddComponent<TilemapRenderer>();
            }

            TilemapRenderer groundRenderer = tilemap == null ? null : tilemap.GetComponent<TilemapRenderer>();
            if (groundRenderer != null)
            {
                renderer.sortingLayerID = groundRenderer.sortingLayerID;
                renderer.sortingOrder = groundRenderer.sortingOrder + 1;
            }

            Material material = waterFoamMaterial;
            if (material == null)
            {
                Shader shader = Shader.Find("LoadGame/Animated Shoreline Foam");
                if (shader != null)
                {
                    runtimeWaterFoamMaterial = new Material(shader)
                    {
                        name = "Animated Shoreline Foam (Runtime)"
                    };
                    material = runtimeWaterFoamMaterial;
                }
                else
                {
                    Debug.LogWarning("Animated shoreline foam shader was not found.", this);
                }
            }

            renderer.sharedMaterial = material;
            renderer.mode = TilemapRenderer.Mode.Chunk;
        }

        private void ConfigureWaterFoamSpriteUv(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null || waterFoamTilemap == null)
            {
                return;
            }

            TilemapRenderer renderer = waterFoamTilemap.GetComponent<TilemapRenderer>();
            Material material = renderer == null ? null : renderer.sharedMaterial;
            if (material == null || !material.HasProperty("_SpriteUVRect"))
            {
                return;
            }

            Rect rect = sprite.textureRect;
            float inverseWidth = 1f / sprite.texture.width;
            float inverseHeight = 1f / sprite.texture.height;
            material.SetVector(
                "_SpriteUVRect",
                new Vector4(
                    rect.xMin * inverseWidth,
                    rect.yMin * inverseHeight,
                    rect.width * inverseWidth,
                    rect.height * inverseHeight));
        }

        private Sprite CreateWaterFoamMaskSprite()
        {
            if (runtimeWaterFoamSprite != null)
            {
                return runtimeWaterFoamSprite;
            }

            const int textureWidth = 128;
            const int textureHeight = 64;
            runtimeWaterFoamTexture = new Texture2D(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                mipChain: false)
            {
                name = "Water Foam Diamond Mask",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[textureWidth * textureHeight];
            Color32 opaque = new Color32(255, 255, 255, 255);
            Color32 transparent = new Color32(255, 255, 255, 0);
            for (int y = 0; y < textureHeight; y++)
            {
                float normalizedY = ((y + 0.5f) / textureHeight - 0.5f) * 2f;
                for (int x = 0; x < textureWidth; x++)
                {
                    float normalizedX = ((x + 0.5f) / textureWidth - 0.5f) * 2f;
                    pixels[y * textureWidth + x] = Mathf.Abs(normalizedX) + Mathf.Abs(normalizedY) <= 1f
                        ? opaque
                        : transparent;
                }
            }

            runtimeWaterFoamTexture.SetPixels32(pixels);
            runtimeWaterFoamTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            float pixelsPerUnit = textureWidth / Mathf.Max(0.0001f, tileWidth);
            runtimeWaterFoamSprite = Sprite.Create(
                runtimeWaterFoamTexture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                extrude: 0,
                SpriteMeshType.FullRect);
            runtimeWaterFoamSprite.name = "Water Foam Diamond Mask";
            return runtimeWaterFoamSprite;
        }

        private void EnsureGroundDecorationTilemap()
        {
            if (groundDecorationTilemap != null || grid == null)
            {
                return;
            }

            Transform existing = grid.transform.Find("Ground Decorations");
            GameObject tilemapObject;
            if (existing != null)
            {
                tilemapObject = existing.gameObject;
                groundDecorationTilemap = tilemapObject.GetComponent<Tilemap>();
            }
            else
            {
                tilemapObject = new GameObject("Ground Decorations");
                tilemapObject.transform.SetParent(grid.transform, worldPositionStays: false);
                groundDecorationTilemap = tilemapObject.AddComponent<Tilemap>();
            }

            if (groundDecorationTilemap == null)
            {
                groundDecorationTilemap = tilemapObject.AddComponent<Tilemap>();
            }

            TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = tilemapObject.AddComponent<TilemapRenderer>();
            }

            TilemapRenderer groundRenderer = tilemap == null ? null : tilemap.GetComponent<TilemapRenderer>();
            if (groundRenderer != null)
            {
                renderer.sortingLayerID = groundRenderer.sortingLayerID;
                renderer.sortingOrder = groundRenderer.sortingOrder + 1;
                renderer.sharedMaterial = groundRenderer.sharedMaterial;
            }

            renderer.mode = TilemapRenderer.Mode.Chunk;
        }

        private void PrewarmRoadEffectPool()
        {
            if (roadRevealParticlePrefab == null || roadEffectPool.Count > 0)
            {
                return;
            }

            for (int i = 0; i < roadEffectPoolSize; i++)
            {
                ParticleSystem particleSystem = Instantiate(roadRevealParticlePrefab, decorationRoot);
                BuildingPlacementDustParticle particle = particleSystem.GetComponent<BuildingPlacementDustParticle>();
                if (particle == null)
                {
                    particle = particleSystem.gameObject.AddComponent<BuildingPlacementDustParticle>();
                }

                particle.Initialize(particleSystem, ReturnRoadEffect);
                roadEffectPool.Enqueue(particle);
                createdRoadEffects++;
            }
        }

        private void PlayRoadRevealEffect(Vector3 worldPosition)
        {
            if (roadEffectPool.Count > 0)
            {
                roadEffectPool.Dequeue().Play(worldPosition);
            }
        }

        private void ReturnRoadEffect(BuildingPlacementDustParticle particle)
        {
            particle.ResetForPool();
            roadEffectPool.Enqueue(particle);
        }

        private static void ConfigureRendererMode(Tilemap sourceTilemap, TilemapRenderer.Mode mode)
        {
            TilemapRenderer tilemapRenderer = sourceTilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null)
            {
                tilemapRenderer.mode = mode;
            }
        }

        public void ClearHighlight()
        {
            if (purchasableHintCoroutine != null)
            {
                StopCoroutine(purchasableHintCoroutine);
                purchasableHintCoroutine = null;
            }

            ClearHighlightImmediate();
        }

        private void ClearHighlightImmediate()
        {
            if (highlightTilemap != null)
            {
                highlightTilemap.ClearAllTiles();
                highlightTilemap.color = highlightTilemapBaseColor;
            }

            highlightedCell = null;
        }

        private Vector2Int WorldToChunk(Vector3 worldPosition)
        {
            float gridX = worldPosition.x / tileWidth + worldPosition.y / tileHeight;
            float gridY = worldPosition.y / tileHeight - worldPosition.x / tileWidth;
            return new Vector2Int(Mathf.FloorToInt(gridX / mapData.ChunkSize), Mathf.FloorToInt(gridY / mapData.ChunkSize));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!debugMap || worldSettings == null)
            {
                return;
            }

            int minX = -Mathf.FloorToInt(worldSettings.startAreaWidth * 0.5f);
            int minY = -Mathf.FloorToInt(worldSettings.startAreaHeight * 0.5f);
            Vector3 startA = GridToWorldForGizmo(minX, minY);
            Vector3 startB = GridToWorldForGizmo(minX + worldSettings.startAreaWidth, minY);
            Vector3 startC = GridToWorldForGizmo(minX + worldSettings.startAreaWidth, minY + worldSettings.startAreaHeight);
            Vector3 startD = GridToWorldForGizmo(minX, minY + worldSettings.startAreaHeight);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startA, startB);
            Gizmos.DrawLine(startB, startC);
            Gizmos.DrawLine(startC, startD);
            Gizmos.DrawLine(startD, startA);

            if (mapData == null)
            {
                return;
            }

            foreach (GridCellData cell in mapData.Cells)
            {
                if (cell.CellType == GridCellType.Water)
                {
                    Gizmos.color = new Color(0f, 0.65f, 1f, 0.8f);
                }
                else if (mapData.IsRoad(cell.x, cell.y))
                {
                    Gizmos.color = new Color(1f, 0f, 1f, 0.65f);
                }
                else
                {
                    if (mapData.IsDeveloped(cell.x, cell.y))
                    {
                        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
                    }
                    else if (mapData.IsRevealed(cell.x, cell.y))
                    {
                        Gizmos.color = new Color(1f, 0.85f, 0f, 0.25f);
                    }
                    else
                    {
                        Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.2f);
                    }
                }

                Gizmos.DrawCube(GridToWorldForGizmo(cell.x, cell.y), new Vector3(tileWidth * 0.18f, tileHeight * 0.18f, 0.01f));
            }
        }

        private Vector3 GridToWorldForGizmo(int x, int y)
        {
            if (tilemap != null)
            {
                return tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
            }

            return transform.TransformPoint(new Vector3((x - y) * tileWidth * 0.5f, (x + y) * tileHeight * 0.5f, 0f));
        }
#endif
    }
}
