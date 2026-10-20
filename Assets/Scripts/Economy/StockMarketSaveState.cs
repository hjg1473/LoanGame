using System;

namespace LoadGame.Economy
{
    [Serializable]
    public sealed class StockMarketSaveState
    {
        public double stockPrice;
        public long ownedShares;
        public double averageBuyPrice;
    }
}
