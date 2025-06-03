using System.Globalization;

namespace Doppler.Billing.Job.Extensions
{
    public static class Parse
    {
        public static double ToDouble(this string value)
        {
            if (string.IsNullOrEmpty(value.Trim())) return 0;
            return double.Parse(value, NumberStyles.AllowDecimalPoint, new CultureInfo("en-US", false));
        }
    }
}
