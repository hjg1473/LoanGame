using System;
using System.Globalization;

namespace LoadGame.UI
{
    public static class CompactNumberFormatter
    {
        public static string Format(double value)
        {
            double absolute = Math.Abs(value);
            if (absolute < 1_000_000d)
            {
                return value.ToString("N0", CultureInfo.CurrentCulture);
            }

            string suffix;
            double divisor;
            if (absolute >= 1_000_000_000_000d)
            {
                suffix = "T";
                divisor = 1_000_000_000_000d;
            }
            else if (absolute >= 1_000_000_000d)
            {
                suffix = "B";
                divisor = 1_000_000_000d;
            }
            else
            {
                suffix = "M";
                divisor = 1_000_000d;
            }

            return (value / divisor).ToString("0.##", CultureInfo.InvariantCulture) + suffix;
        }
    }
}
