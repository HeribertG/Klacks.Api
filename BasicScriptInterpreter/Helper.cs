using System.Globalization;

namespace Klacks.Api.BasicScriptInterpreter
{
    public static class Helper
    {
        public static bool IsNumericDouble(object value)
        {
            return double.TryParse(value.ToString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);
        }

        public static bool IsNumericInt(object value)
        {
            return int.TryParse(value.ToString(), out _);
        }

        public static double ExtractDouble(object value)
        {
            if (value is Identifier identifier)
            {
                return Convert.ToDouble(identifier.Value, CultureInfo.InvariantCulture);
            }

            if (IsNumericDouble(value))
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            return 0.0;
        }

        public static int ExtractInt(object value)
        {
            if (value is Identifier identifier)
            {
                return Convert.ToInt32(identifier.Value);
            }

            if (IsNumericInt(value))
            {
                return Convert.ToInt32(value);
            }

            return 0;
        }

        public static string ExtractString(object value)
        {
            if (value is Identifier identifier)
            {
                return Convert.ToString(identifier.Value) ?? string.Empty;
            }

            return Convert.ToString(value) ?? string.Empty;
        }
    }
}
