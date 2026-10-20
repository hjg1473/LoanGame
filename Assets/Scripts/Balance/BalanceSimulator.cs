using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LoadGame.Balance
{
    public enum BotStrategy { Safe, Balanced, Aggressive }
    public enum SimulationOutcome { Timeout, Victory, Bankruptcy }

    public sealed class BalanceSimulationConfig
    {
        public int MaxSeconds = 2100;
        public double InterestRate = 0.00015d;
        public long BankruptcyDebt = 250000L;
        public int SnapshotInterval = 10;
    }

    public sealed class BalanceSnapshot
    {
        public int Second;
        public long Cash;
        public long Debt;
        public double Production;
        public double Maintenance;
        public double Population;
        public double Employment;
        public float Happiness;
        public int CompletedProjects;
    }

    public sealed class BalanceRunResult
    {
        public int Seed;
        public BotStrategy Strategy;
        public SimulationOutcome Outcome;
        public int DurationSeconds;
        public long FinalCash;
        public long FinalDebt;
        public long MaxDebt;
        public int CompletedProjects;
        public int Decisions;
        public int MaximumIdleSeconds;
        public readonly List<int> ProjectCompletionSeconds = new List<int>();
        public readonly List<BalanceSnapshot> Snapshots = new List<BalanceSnapshot>();
    }

    public sealed class BalanceBatchResult
    {
        public readonly List<BalanceRunResult> Runs = new List<BalanceRunResult>();
        public int Victories;
        public int Bankruptcies;
        public int Timeouts;
    }

    public static class BalanceSimulator
    {
        public static BalanceRunResult Run(int seed, BotStrategy strategy, BalanceSimulationConfig config = null)
        {
            config ??= new BalanceSimulationConfig();
            DeterministicRandom random = new DeterministicRandom(seed);
            BalanceRunResult result = new BalanceRunResult { Seed = seed, Strategy = strategy };
            long cash = 2000L;
            long debt = 0L;
            long maxDebt = 0L;
            double population = 0d;
            long capacity = 0L;
            long jobs = 0L;
            long leisure = 0L;
            double production = 0d;
            double maintenance = 0d;
            double cityValue = 0d;
            int completedProjects = 0;
            double interestBuffer = 0d;
            float positiveNetSeconds = 0f;
            int buildCooldown = 0;
            int decisions = 0;
            int lastDecisionSecond = 0;
            int maximumIdleSeconds = 0;

            for (int second = 1; second <= config.MaxSeconds; second++)
            {
                if (buildCooldown-- <= 0)
                {
                    if (BuildForStrategy(strategy, random, second, population, ref cash, ref debt, ref capacity, ref jobs, ref leisure, ref production, ref maintenance, ref cityValue))
                    {
                        decisions++;
                        maximumIdleSeconds = Math.Max(maximumIdleSeconds, second - lastDecisionSecond);
                        lastDecisionSecond = second;
                    }
                    buildCooldown = strategy == BotStrategy.Aggressive ? 24 : strategy == BotStrategy.Balanced ? 26 : 28;
                }

                PopulationDerivedState derived = PopulationRules.Derive(population, jobs, leisure, 40f);
                population = Math.Min(capacity, population + PopulationRules.MoveInRate(derived.Happiness, 35f, 80f, 0.1d, 0.45d, 0.9d));
                derived = PopulationRules.Derive(population, jobs, leisure, 40f);
                double effectiveProduction = production * derived.EmploymentEfficiency;
                OperatingFlow flow = EconomyRules.ResolveWholeOperatingFlow(cash, (long)effectiveProduction, (long)maintenance);
                cash = flow.CashAfter;
                debt = SafeAdd(debt, flow.DebtAdded);
                debt = SafeAdd(debt, EconomyRules.AccruedWholeInterest(debt, config.InterestRate, 1d, ref interestBuffer));
                double net = EconomyRules.NetIncomePerSecond(effectiveProduction, maintenance, debt, config.InterestRate);
                positiveNetSeconds = net > 0d ? positiveNetSeconds + 1f : 0f;
                maxDebt = Math.Max(maxDebt, debt);

                while (completedProjects < 4 && cityValue >= ProjectCityValue(completedProjects) && cash + AvailableBorrow(strategy, debt) >= ProjectCost(completedProjects))
                {
                    Pay(ProjectCost(completedProjects), ref cash, ref debt);
                    completedProjects++;
                    result.ProjectCompletionSeconds.Add(second);
                    cityValue += ProjectCityValue(completedProjects - 1) * 0.2d;
                }

                ProjectRequirementState finalState = new ProjectRequirementState(cityValue, population, derived.EmploymentEfficiency, derived.Happiness, positiveNetSeconds);
                if (completedProjects == 4
                    && ProjectRules.MeetsNumericRequirements(finalState, 70000d, 350d, 0.85f, 80f, 60f)
                    && cash + AvailableBorrow(strategy, debt) >= 100000L)
                {
                    Pay(100000L, ref cash, ref debt);
                    completedProjects = 5;
                    result.ProjectCompletionSeconds.Add(second);
                    result.Outcome = SimulationOutcome.Victory;
                    result.DurationSeconds = second;
                    break;
                }

                if (debt >= config.BankruptcyDebt && net <= 0d)
                {
                    result.Outcome = SimulationOutcome.Bankruptcy;
                    result.DurationSeconds = second;
                    break;
                }

                if (second % Math.Max(1, config.SnapshotInterval) == 0)
                {
                    result.Snapshots.Add(new BalanceSnapshot { Second = second, Cash = cash, Debt = debt, Production = effectiveProduction, Maintenance = maintenance, Population = population, Employment = derived.EmploymentEfficiency, Happiness = derived.Happiness, CompletedProjects = completedProjects });
                }

                result.DurationSeconds = second;
            }

            result.FinalCash = cash;
            result.FinalDebt = debt;
            result.MaxDebt = maxDebt;
            result.CompletedProjects = completedProjects;
            result.Decisions = decisions;
            result.MaximumIdleSeconds = Math.Max(maximumIdleSeconds, result.DurationSeconds - lastDecisionSecond);
            return result;
        }

        public static BalanceBatchResult RunBatch(int count, int baseSeed = 1000)
        {
            BalanceBatchResult batch = new BalanceBatchResult();
            for (int i = 0; i < Math.Max(1, count); i++)
            {
                BotStrategy strategy = (BotStrategy)(i % 3);
                BalanceRunResult run = Run(baseSeed + i, strategy);
                batch.Runs.Add(run);
                if (run.Outcome == SimulationOutcome.Victory) batch.Victories++;
                else if (run.Outcome == SimulationOutcome.Bankruptcy) batch.Bankruptcies++;
                else batch.Timeouts++;
            }
            return batch;
        }

        public static void WriteCsv(BalanceBatchResult batch, string directory)
        {
            Directory.CreateDirectory(directory);
            StringBuilder summary = new StringBuilder("seed,strategy,outcome,duration,finalCash,finalDebt,maxDebt,completedProjects,decisions,maximumIdleSeconds,projectCompletionSeconds\n");
            StringBuilder snapshots = new StringBuilder("seed,strategy,second,cash,debt,production,maintenance,population,employment,happiness,completedProjects\n");
            foreach (BalanceRunResult run in batch.Runs)
            {
                summary.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},\"{10}\"\n", run.Seed, run.Strategy, run.Outcome, run.DurationSeconds, run.FinalCash, run.FinalDebt, run.MaxDebt, run.CompletedProjects, run.Decisions, run.MaximumIdleSeconds, string.Join("|", run.ProjectCompletionSeconds));
                foreach (BalanceSnapshot s in run.Snapshots)
                {
                    snapshots.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5:0.###},{6:0.###},{7:0.###},{8:0.####},{9:0.##},{10}\n", run.Seed, run.Strategy, s.Second, s.Cash, s.Debt, s.Production, s.Maintenance, s.Population, s.Employment, s.Happiness, s.CompletedProjects);
                }
            }
            File.WriteAllText(Path.Combine(directory, "balance_summary.csv"), summary.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "balance_snapshots.csv"), snapshots.ToString(), Encoding.UTF8);
        }

        private static bool BuildForStrategy(BotStrategy strategy, DeterministicRandom random, int second, double population, ref long cash, ref long debt, ref long capacity, ref long jobs, ref long leisure, ref double production, ref double maintenance, ref double cityValue)
        {
            double debtTolerance = strategy == BotStrategy.Safe ? 0d : strategy == BotStrategy.Balanced ? 0.5d : 1d;
            bool needHousing = capacity < 10L || jobs > 0L && capacity < jobs + 20L || population > capacity * 0.9d;
            bool needWork = jobs < 10L || jobs < population * 1.05d;
            bool needLeisure = population >= 0.5d && leisure < Math.Max(10d, population * 0.9d);
            long cost; int cap = 0, worker = 0, fun = 0; double prod = 0d, upkeep = 0d, value;
            if (needHousing)
            {
                bool late = second >= 600 && cash + (long)(12000L * debtTolerance) >= 12000L;
                cost = late ? 12000L : 500L; cap = late ? 80 : 10; upkeep = late ? 30d : 2d; value = cost;
            }
            else if (needWork)
            {
                bool late = second >= 900 && cash + (long)(30000L * debtTolerance) >= 30000L;
                bool mid = !late && second >= 300 && cash + (long)(6000L * debtTolerance) >= 6000L;
                cost = late ? 30000L : mid ? 6000L : 1500L;
                worker = late ? 100 : mid ? 25 : 10;
                prod = (late ? 750d : mid ? 75d : 30d) * (0.92d + random.NextDouble() * 0.16d);
                upkeep = late ? 220d : mid ? 18d : 5d; value = cost;
            }
            else if (needLeisure)
            {
                bool late = second >= 900 && cash + (long)(20000L * debtTolerance) >= 20000L;
                cost = late ? 20000L : 2500L; fun = late ? 120 : 35; upkeep = late ? 80d : 8d; value = cost;
            }
            else
            {
                return false;
            }
            long borrowLimit = Math.Max(0L, 120000L - debt);
            long financingAllowance = Math.Min(borrowLimit, (long)(cost * debtTolerance) + 100L);
            if (cash + financingAllowance < cost) return false;
            Pay(cost, ref cash, ref debt);
            capacity += cap; jobs += worker; leisure += fun; production += prod; maintenance += upkeep; cityValue += value;
            return true;
        }

        private static long AvailableBorrow(BotStrategy strategy, long debt) => strategy == BotStrategy.Safe ? 0L : Math.Max(0L, 120000L - debt);
        private static long ProjectCost(int index) => new[] { 2000L, 8000L, 20000L, 40000L }[index];
        private static double ProjectCityValue(int index) => new[] { 1000d, 5000d, 15000d, 35000d }[index];
        private static void Pay(long amount, ref long cash, ref long debt) { long fromCash = Math.Min(cash, amount); cash -= fromCash; debt = SafeAdd(debt, amount - fromCash); }
        private static long SafeAdd(long a, long b) => b > 0L && a > long.MaxValue - b ? long.MaxValue : a + b;

        private struct DeterministicRandom
        {
            private uint state;
            public DeterministicRandom(int seed) { state = (uint)(seed == 0 ? 0x6D2B79F5 : seed); }
            public double NextDouble() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return state / (double)uint.MaxValue; }
        }
    }
}
