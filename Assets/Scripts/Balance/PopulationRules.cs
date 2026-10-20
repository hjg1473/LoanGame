using System;

namespace LoadGame.Balance
{
    public readonly struct PopulationDerivedState
    {
        public PopulationDerivedState(double employed, double employment, double leisure, float happiness)
        {
            EmployedPopulation = employed;
            EmploymentEfficiency = employment;
            LeisureCoverage = leisure;
            Happiness = happiness;
        }
        public double EmployedPopulation { get; }
        public double EmploymentEfficiency { get; }
        public double LeisureCoverage { get; }
        public float Happiness { get; }
    }

    public static class PopulationRules
    {
        public static PopulationDerivedState Derive(double population, long requiredWorkers, long leisureCapacity, float minHappiness)
        {
            population = Math.Max(0d, population);
            double employed = Math.Min(population, Math.Max(0L, requiredWorkers));
            double employment = requiredWorkers > 0L ? Clamp01(employed / requiredWorkers) : 0d;
            double leisure = population > 0d ? Clamp01(Math.Max(0L, leisureCapacity) / population) : 1d;
            float happiness = (float)Math.Min(100d, Math.Max(0d, minHappiness + (100d - minHappiness) * leisure));
            return new PopulationDerivedState(employed, employment, leisure, happiness);
        }

        public static double MoveInRate(float happiness, float stoppedThreshold, float highThreshold, double lowRate, double normalRate, double highRate)
        {
            double stop = Math.Min(100d, Math.Max(0d, stoppedThreshold));
            double high = Math.Min(100d, Math.Max(stop + 0.01d, highThreshold));
            if (happiness <= stop) return 0d;
            if (happiness < high) return Lerp(lowRate, normalRate, (happiness - stop) / (high - stop));
            return Lerp(normalRate, highRate, high >= 100d ? 1d : (happiness - high) / (100d - high));
        }

        private static double Clamp01(double value) => Math.Min(1d, Math.Max(0d, value));
        private static double Lerp(double left, double right, double t) => left + (right - left) * Clamp01(t);
    }
}
