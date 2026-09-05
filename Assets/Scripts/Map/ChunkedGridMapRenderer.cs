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
        [SerializeField] private ProceduralRoadNetwork proceduralRoadNetwork = new ProceduralRoadNetwork();

        [Header("Tilemaps")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private Tilemap buildingTilemap;
        [SerializeField] private Tilemap highlightTilemap;

        [Header("Tile Size")]
        [SerializeField] private float tileWidth = 1f;
        [SerializeField] private float tileHeight = 0.5f;

        [Header("Sprites")]
        [SerializeField] private string emptySpritePath = "Sprites/Grass1";
        [SerializeField] private string roadSpritePath = "Sprites/Load1";
        [SerializeField] private string ownedSpritePath = "Sprites/Building1";
        [SerializeField] private string highlightSpritePath = "Sprites/Highlight1";
        [SerializeField, Range(0f, 1f)] private float buildingAlpha = 1f;
        [SerializeField] private BuildingDefinition[] buildingDefinitions =
        {
            new BuildingDefinition(),
            new BuildingDefinition
            {
                id = "factory",
                displayName = "Factory",
                price = 2000L,
                cashPerSecond = 60d,
                width = 1,
                height = 1,
                spritePath = "Sprites/Building2"
            },
            new BuildingDefinition
            {
                id = "tower",
                displayName = "Tower",
                price = 10000L,
                cashPerSecond = 400d,
                width = 1,
                height = 1,
                spritePath = "Sprites/Building2"
            }
        };
        [SerializeField] private RoadVisualDefinition[] roadVisualDefinitions;
        [SerializeField] private bool debugMap;

        private readonly HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();
        private readonly Dictionary<int, TileBase> roadTilesByMask = new Dictionary<int, TileBase>();
        private readonly Dictionary<string, TileBase> buildingTiles = new Dictionary<string, TileBase>();
        private readonly Dictionary<string, BuildingDefinition> buildingDefinitionsById = new Dictionary<string, BuildingDefinition>();

        private GridMapData mapData;
        private TileBase emptyTile;
        private TileBase roadTile;
        private TileBase ownedTile;
        private TileBase highlightTile;
        private RoadVisualResolver roadVisualResolver;
        private Vector3Int? highlightedCell;
        private Vector2Int lastCameraChunk = new Vector2Int(int.MinValue, int.MinValue);

        public GridMapData MapData => mapData;
        public float TileWidth => tileWidth;
        public float TileHeight => tileHeight;
        public BuildingDefinition[] BuildingDefinitions => buildingDefinitions;

        public void Initialize(GridMapData data, Camera camera)
        {
            mapData = data;
            targetCamera = camera;
            mapData.SetProceduralRoadNetwork(proceduralRoadNetwork);
            EnsureDefaultBuildingDefinitions();
            EnsureDefaultRoadVisualDefinitions();
            ConfigureTilemaps();
            CreateTiles();
            roadVisualResolver = new RoadVisualResolver(roadVisualDefinitions);
            CreateRoadTiles();
            RefreshVisibleChunks(force: true);

            if (debugMap)
            {
                Debug.Log($"ChunkedGridMapRenderer initialized. chunkSize={mapData.ChunkSize}, activeChunks={activeChunks.Count}, tilemap={tilemap?.name}, highlightTilemap={highlightTilemap?.name}", this);
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
                return;
            }

            lastCameraChunk = cameraChunk;
            HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();

            for (int y = cameraChunk.y - visibleChunkRadius; y <= cameraChunk.y + visibleChunkRadius; y++)
            {
                for (int x = cameraChunk.x - visibleChunkRadius; x <= cameraChunk.x + visibleChunkRadius; x++)
                {
                    Vector2Int chunk = new Vector2Int(x, y);
                    neededChunks.Add(chunk);

                    if (!activeChunks.Contains(chunk))
                    {
                        LoadChunk(chunk);
                        activeChunks.Add(chunk);
                    }
                }
            }

            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            foreach (Vector2Int chunk in activeChunks)
            {
                if (!neededChunks.Contains(chunk))
                {
                    chunksToUnload.Add(chunk);
                }
            }

            foreach (Vector2Int chunk in chunksToUnload)
            {
                UnloadChunk(chunk);
                activeChunks.Remove(chunk);
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

            if (buildingTilemap != null)
            {
                buildingTilemap.ClearAllTiles();
                ConfigureRendererMode(buildingTilemap, TilemapRenderer.Mode.Individual);
            }

            if (highlightTilemap != null)
            {
                highlightTilemap.ClearAllTiles();
                ConfigureRendererMode(highlightTilemap, TilemapRenderer.Mode.Chunk);
            }

            activeChunks.Clear();
            highlightedCell = null;
            lastCameraChunk = new Vector2Int(int.MinValue, int.MinValue);
        }

        private void CreateTiles()
        {
            emptyTile = CreateTile(LoadSprite(emptySpritePath));
            roadTile = CreateTile(LoadSprite(roadSpritePath));
            ownedTile = CreateTile(LoadSprite(ownedSpritePath));
            highlightTile = CreateTile(LoadSprite(highlightSpritePath));
            buildingTiles.Clear();
            buildingDefinitionsById.Clear();

            foreach (BuildingDefinition definition in buildingDefinitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.id))
                {
                    continue;
                }

                buildingDefinitionsById[definition.id] = definition;
                buildingTiles[definition.id] = CreateTile(LoadSprite(definition.spritePath), new Color(1f, 1f, 1f, buildingAlpha));
            }
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
            tile.sprite = LoadSprite(definition.spritePath);
            tile.color = Color.white;
            tile.flags = TileFlags.None;
            tile.transform = definition.flipY180
                ? Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one)
                : Matrix4x4.identity;
            return tile;
        }

        private void EnsureDefaultBuildingDefinitions()
        {
            if (buildingDefinitions != null && buildingDefinitions.Length >= 3)
            {
                return;
            }

            buildingDefinitions = new[]
            {
                new BuildingDefinition
                {
                    id = "small_office",
                    displayName = "Small Office",
                    price = 500L,
                    cashPerSecond = 10d,
                    width = 1,
                    height = 1,
                    spritePath = "Sprites/Building2"
                },
                new BuildingDefinition
                {
                    id = "factory",
                    displayName = "Factory",
                    price = 2000L,
                    cashPerSecond = 60d,
                    width = 1,
                    height = 1,
                    spritePath = "Sprites/Building2"
                },
                new BuildingDefinition
                {
                    id = "tower",
                    displayName = "Tower",
                    price = 10000L,
                    cashPerSecond = 400d,
                    width = 1,
                    height = 1,
                    spritePath = "Sprites/Building2"
                }
            };
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
                new RoadVisualDefinition { connectionMask = 10, spritePath = "Sprites/Load2" },
                new RoadVisualDefinition { connectionMask = 3, spritePath = "Sprites/Corner1" },
                new RoadVisualDefinition { connectionMask = 6, spritePath = "Sprites/Corner2" },
                new RoadVisualDefinition { connectionMask = 12, spritePath = "Sprites/Corner3" },
                new RoadVisualDefinition { connectionMask = 9, spritePath = "Sprites/Corner4" },
                new RoadVisualDefinition { connectionMask = 7, spritePath = "Sprites/LoadT" },
                new RoadVisualDefinition { connectionMask = 11, spritePath = "Sprites/LoadT", flipY180 = true },
                new RoadVisualDefinition { connectionMask = 13, spritePath = "Sprites/LoadT" },
                new RoadVisualDefinition { connectionMask = 14, spritePath = "Sprites/LoadT", flipY180 = true },
                new RoadVisualDefinition { connectionMask = 15, spritePath = "Sprites/LoadT" }
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
                    if (cell.CellType == GridCellType.Road)
                    {
                        tilemap.SetTile(tilePosition, GetRoadTile(GetRoadConnectionMask(cell.x, cell.y)));
                        SetBuildingTile(tilePosition, null);
                        continue;
                    }

                    tilemap.SetTile(tilePosition, GetGroundTile(cell));
                    SetBuildingTile(tilePosition, GetBuildingTile(cell));
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
                    SetBuildingTile(tilePosition, null);
                }
            }
        }

        private TileBase GetGroundTile(GridCellData cell)
        {
            if (cell.owned)
            {
                return ownedTile;
            }

            return cell.CellType == GridCellType.Road ? roadTile : emptyTile;
        }

        private TileBase GetBuildingTile(GridCellData cell)
        {
            if (!string.IsNullOrEmpty(cell.buildingId) && buildingTiles.TryGetValue(cell.buildingId, out TileBase buildingTile))
            {
                return buildingTile;
            }

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
        }

        public void SetCellOwned(GridCellData cell, bool owned)
        {
            mapData.SetOwned(cell.x, cell.y, owned);
            RefreshVisibleCell(cell);
        }

        public void SetCellBuilding(GridCellData cell, string buildingId)
        {
            mapData.SetBuilding(cell.x, cell.y, buildingId);
            RefreshVisibleCell(cell);
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
            return definition == null ? null : LoadSprite(definition.spritePath);
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
                tilemap.SetTile(tilePosition, GetGroundTile(cell));
                SetBuildingTile(tilePosition, GetBuildingTile(cell));
            }
        }

        private void SetBuildingTile(Vector3Int tilePosition, TileBase tile)
        {
            if (buildingTilemap != null)
            {
                buildingTilemap.SetTile(tilePosition, tile);
            }
            else if (tile != null)
            {
                tilemap.SetTile(tilePosition, tile);
            }
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
            if (!highlightedCell.HasValue)
            {
                return;
            }

            if (highlightTilemap != null)
            {
                highlightTilemap.SetTile(highlightedCell.Value, null);
            }

            highlightedCell = null;
        }

        private Vector2Int WorldToChunk(Vector3 worldPosition)
        {
            float gridX = worldPosition.x / tileWidth + worldPosition.y / tileHeight;
            float gridY = worldPosition.y / tileHeight - worldPosition.x / tileWidth;
            return new Vector2Int(Mathf.FloorToInt(gridX / mapData.ChunkSize), Mathf.FloorToInt(gridY / mapData.ChunkSize));
        }
    }
}
