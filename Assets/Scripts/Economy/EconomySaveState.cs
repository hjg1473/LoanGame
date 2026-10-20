using System;

namespace LoadGame.Economy
{
    [Serializable]
    public sealed class EconomySaveState
    {
        public long cash;
        public long debt;
        public double interestRate;
        public float survivalTimeSeconds;
        public double highestCashPerSecond;
    }
}
