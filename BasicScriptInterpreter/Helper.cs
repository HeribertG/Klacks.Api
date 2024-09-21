using System.Globalization;

namespace Klacks.Api.BasicScriptInterpreter
{
  public static class Helper
  {
    public static bool IsNumericDouble(object value)
    {
      if (double.TryParse(value.ToString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public static bool IsNumericInt(object value)
    {
      if (int.TryParse(value.ToString(), out _))
      {
        return true;
      }
      else
      {
        return false;
      }
    }
  }
}
