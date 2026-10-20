namespace LoadGame.Economy
{
    public readonly struct EconomyGameOverStats
    {
        public readonly long FinalCash;
        public readonly long FinalDebt;
        public readonly double HighestCashPerSecond;
        public readonly float SurvivalTimeSeconds;

        public EconomyGameOverStats(long finalCash, long finalDebt, double highestCashPerSecond, float survivalTimeSeconds)
        {
            FinalCash = finalCash;
            FinalDebt = finalDebt;
            HighestCashPerSecond = highestCashPerSecond;
            SurvivalTimeSeconds = survivalTimeSeconds;
        }
    }
}
