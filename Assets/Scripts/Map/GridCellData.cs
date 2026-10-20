using System;
using UnityEngine;

namespace LoadGame.Map
{
    public enum GridCellType
    {
        Empty,
        Road,
        Water
    }

    [Serializable]
    public sealed class GridCellData
    {
        public int x;
        public int y;
        public string type = "empty";
        public bool owned;
        public string buildingId;
        public string buildingInstanceId;

        public GridCellType CellType
        {
            get
            {
                if (string.Equals(type, "road", StringComparison.OrdinalIgnoreCase))
                {
                    return GridCellType.Road;
                }

                return string.Equals(type, "water", StringComparison.OrdinalIgnoreCase)
                    ? GridCellType.Water
                    : GridCellType.Empty;
            }
        }
    }

    [Serializable]
    public sealed class GridMapJson
    {
        public int chunkSize = 16;
        public RectIntJson cameraBounds = new RectIntJson { xMin = -64, yMin = -64, xMax = 64, yMax = 64 };
        public GridCellData[] cells = Array.Empty<GridCellData>();
    }

    [Serializable]
    public sealed class RectIntJson
    {
        public int xMin;
        public int yMin;
        public int xMax;
        public int yMax;

        public Rect ToWorldBounds(float tileWidth, float tileHeight)
        {
            Vector2 min = GridToWorld(xMin, yMax, tileWidth, tileHeight);
            Vector2 max = GridToWorld(xMax, yMin, tileWidth, tileHeight);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Vector2 GridToWorld(int x, int y, float tileWidth, float tileHeight)
        {
            return new Vector2((x - y) * tileWidth * 0.5f, (x + y) * tileHeight * 0.5f);
        }
    }
}
