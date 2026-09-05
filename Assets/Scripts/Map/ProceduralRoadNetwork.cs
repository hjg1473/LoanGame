using System;
using UnityEngine;

namespace LoadGame.Map
{
    [Serializable]
    public sealed class ProceduralRoadNetwork
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private int worldSeed = 12345;
        [SerializeField] private int blockSize = 8;

        public bool Enabled => enabled;

        public bool IsRoad(int x, int y)
        {
            if (!enabled)
            {
                return false;
            }

            int size = Mathf.Max(4, blockSize);
            bool majorRoad = Mod(x, size) == 0 || Mod(y, size) == 0;
            if (majorRoad)
            {
                return true;
            }

            int blockX = FloorDiv(x, size);
            int blockY = FloorDiv(y, size);
            int localX = Mod(x, size);
            int localY = Mod(y, size);

            int horizontalConnectorY = 2 + PositiveHash(blockX, blockY, 11) % Mathf.Max(1, size - 3);
            int verticalConnectorX = 2 + PositiveHash(blockX, blockY, 29) % Mathf.Max(1, size - 3);

            bool eastConnector = (PositiveHash(blockX, blockY, 43) & 1) == 0;
            bool northConnector = (PositiveHash(blockX, blockY, 71) & 1) == 0;

            return eastConnector && localY == horizontalConnectorY && localX >= size / 2
                || northConnector && localX == verticalConnectorX && localY >= size / 2;
        }

        private int PositiveHash(int x, int y, int salt)
        {
            unchecked
            {
                int hash = worldSeed;
                hash = hash * 73856093 ^ x * 19349663;
                hash = hash * 83492791 ^ y * 297121507;
                hash = hash * 1103515245 + salt;
                return hash & int.MaxValue;
            }
        }

        private static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static int FloorDiv(int value, int divisor)
        {
            int result = value / divisor;
            return value < 0 && value % divisor != 0 ? result - 1 : result;
        }
    }
}
