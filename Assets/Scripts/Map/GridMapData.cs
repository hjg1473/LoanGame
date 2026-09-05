using System.Collections.Generic;
using UnityEngine;

namespace LoadGame.Map
{
    public sealed class GridMapData
    {
        private readonly Dictionary<Vector2Int, GridCellData> cellsByPosition = new Dictionary<Vector2Int, GridCellData>();
        private ProceduralRoadNetwork proceduralRoadNetwork;

        public int ChunkSize { get; }
        public RectIntJson CameraBounds { get; }

        public GridMapData(GridMapJson json)
        {
            ChunkSize = Mathf.Max(1, json.chunkSize);
            CameraBounds = json.cameraBounds;

            foreach (GridCellData cell in json.cells)
            {
                cellsByPosition[new Vector2Int(cell.x, cell.y)] = cell;
            }
        }

        public GridCellData GetCell(int x, int y)
        {
            if (cellsByPosition.TryGetValue(new Vector2Int(x, y), out GridCellData cell))
            {
                return cell;
            }

            cell = new GridCellData
            {
                x = x,
                y = y,
                type = IsProceduralRoad(x, y) ? "road" : "empty",
                owned = false,
                buildingId = null
            };

            cellsByPosition[new Vector2Int(x, y)] = cell;
            return cell;
        }

        public void SetOwned(int x, int y, bool owned)
        {
            GetCell(x, y).owned = owned;
        }

        public void SetBuilding(int x, int y, string buildingId)
        {
            GetCell(x, y).buildingId = buildingId;
        }

        public void SetProceduralRoadNetwork(ProceduralRoadNetwork roadNetwork)
        {
            proceduralRoadNetwork = roadNetwork;
        }

        public bool IsRoad(int x, int y)
        {
            if (cellsByPosition.TryGetValue(new Vector2Int(x, y), out GridCellData cell))
            {
                return cell.CellType == GridCellType.Road;
            }

            return IsProceduralRoad(x, y);
        }

        private bool IsProceduralRoad(int x, int y)
        {
            return proceduralRoadNetwork != null
                && proceduralRoadNetwork.Enabled
                && proceduralRoadNetwork.IsRoad(x, y);
        }
    }
}
