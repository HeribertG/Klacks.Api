using System.Globalization;

namespace Klacks.Api.BasicScriptInterpreter
{
    public static class Formathelper
    {
        public static double FormatDoubleNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            {
                return result;
            }

            var normalizedValue = value.Replace(',', '.');
            if (double.TryParse(normalizedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return 0;
        }
    }
}
