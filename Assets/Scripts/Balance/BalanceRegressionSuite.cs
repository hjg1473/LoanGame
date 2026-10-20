using System;
using System.IO;

namespace LoadGame.Balance
{
    public sealed class BalanceRegressionReport
    {
        public bool Passed;
        public int Assertions;
        public int Runs;
        public int Victories;
        public int Bankruptcies;
        public int Timeouts;
        public string SummaryCsvPath;
        public string SnapshotsCsvPath;
    }

    public static class BalanceRegressionSuite
    {
        public static BalanceRegressionReport RunAll(string outputDirectory, int runCount = 100)
        {
            int assertions = 0;
            Assert(EconomyRules.InterestPerSecond(1000L, 0.01d) == 10d, "Interest rule", ref assertions);
            Assert(EconomyRules.NetIncomePerSecond(30d, 5d, 1000L, 0.01d) == 15d, "Net-income rule", ref assertions);
            Assert(EconomyRules.DemolitionRefund(501L) == 250L, "Demolition floor rule", ref assertions);
            PopulationDerivedState population = PopulationRules.Derive(80d, 100L, 80L, 40f);
            Assert(Math.Abs(population.EmploymentEfficiency - 0.8d) < 0.000001d, "Employment rule", ref assertions);
            Assert(Math.Abs(population.Happiness - 100f) < 0.001f, "Happiness rule", ref assertions);
            BalanceRunResult deterministicA = BalanceSimulator.Run(4242, BotStrategy.Balanced);
            BalanceRunResult deterministicB = BalanceSimulator.Run(4242, BotStrategy.Balanced);
            Assert(deterministicA.FinalCash == deterministicB.FinalCash
                && deterministicA.FinalDebt == deterministicB.FinalDebt
                && deterministicA.Outcome == deterministicB.Outcome,
                "Deterministic seed", ref assertions);

            BalanceBatchResult batch = BalanceSimulator.RunBatch(runCount);
            Assert(batch.Runs.Count == runCount, "Batch run count", ref assertions);
            Assert(batch.Victories + batch.Bankruptcies + batch.Timeouts == runCount, "Outcome accounting", ref assertions);
            foreach (BalanceRunResult run in batch.Runs)
            {
                Assert(run.FinalCash >= 0L && run.FinalDebt >= 0L, "Non-negative economy", ref assertions);
                Assert(run.DurationSeconds > 0 && run.DurationSeconds <= 2100, "Duration bound", ref assertions);
                Assert(run.Decisions > 0 && run.MaximumIdleSeconds > 0, "Playtest observation metrics", ref assertions);
            }

            BalanceSimulationConfig shock = new BalanceSimulationConfig { InterestRate = 0.01d, BankruptcyDebt = 50000L };
            BalanceRunResult shockRun = BalanceSimulator.Run(99173, BotStrategy.Aggressive, shock);
            Assert(shockRun.Outcome == SimulationOutcome.Bankruptcy, "Rate-shock bankruptcy path", ref assertions);

            BalanceSimulator.WriteCsv(batch, outputDirectory);
            string summary = Path.Combine(outputDirectory, "balance_summary.csv");
            string snapshots = Path.Combine(outputDirectory, "balance_snapshots.csv");
            Assert(File.Exists(summary) && File.Exists(snapshots), "CSV output", ref assertions);
            return new BalanceRegressionReport
            {
                Passed = true,
                Assertions = assertions,
                Runs = batch.Runs.Count,
                Victories = batch.Victories,
                Bankruptcies = batch.Bankruptcies,
                Timeouts = batch.Timeouts,
                SummaryCsvPath = summary,
                SnapshotsCsvPath = snapshots
            };
        }

        private static void Assert(bool condition, string name, ref int count)
        {
            count++;
            if (!condition) throw new InvalidOperationException("Balance regression failed: " + name);
        }
    }
}
