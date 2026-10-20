using System;

namespace LoadGame.Balance
{
    public readonly struct OperatingFlow
    {
        public OperatingFlow(long income, long maintenance, long cashAfter, long debtAdded)
        {
            Income = income;
            Maintenance = maintenance;
            CashAfter = cashAfter;
            DebtAdded = debtAdded;
        }

        public long Income { get; }
        public long Maintenance { get; }
        public long CashAfter { get; }
        public long DebtAdded { get; }
    }

    public static class EconomyRules
    {
        public static double InterestPerSecond(long debt, double rate)
        {
            return Math.Max(0L, debt) * Math.Max(0d, rate);
        }

        public static double NetIncomePerSecond(double production, double maintenance, long debt, double rate)
        {
            return Math.Max(0d, production) - Math.Max(0d, maintenance) - InterestPerSecond(debt, rate);
        }

        public static OperatingFlow ResolveWholeOperatingFlow(long cash, long income, long maintenance)
        {
            cash = Math.Max(0L, cash);
            income = Math.Max(0L, income);
            maintenance = Math.Max(0L, maintenance);
            long paidFromCash = Math.Min(cash, maintenance);
            cash -= paidFromCash;
            long remainingMaintenance = maintenance - paidFromCash;
            long paidFromIncome = Math.Min(income, remainingMaintenance);
            income -= paidFromIncome;
            remainingMaintenance -= paidFromIncome;
            return new OperatingFlow(income, maintenance, SafeAdd(cash, income), remainingMaintenance);
        }

        public static long AccruedWholeInterest(long debt, double rate, double elapsedSeconds, ref double buffer)
        {
            buffer += InterestPerSecond(debt, rate) * Math.Max(0d, elapsedSeconds);
            long whole = buffer >= long.MaxValue ? long.MaxValue : (long)buffer;
            buffer -= whole;
            return whole;
        }

        public static long DemolitionRefund(long purchasePrice, double fraction = 0.5d)
        {
            return Math.Max(0L, (long)Math.Floor(Math.Max(0L, purchasePrice) * Math.Max(0d, fraction)));
        }

        private static long SafeAdd(long left, long right)
        {
            return right > 0L && left > long.MaxValue - right ? long.MaxValue : left + right;
        }
    }
}
