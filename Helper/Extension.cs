using Klacks_api.Datas;
using System.Reflection;

namespace Klacks_api.Helper
{
  public static class Extension
  {
    public static string Fallback(this MultiLanguage item, string language)
    {
      var result = string.Empty;

      if (item != null && !string.IsNullOrEmpty(language))
      {
        var propInfo = typeof(MultiLanguage).GetProperty(language);
        if (propInfo != null)
        {
          var res = (string?)propInfo.GetValue(item, null);
        }
      }

      return result;
    }

    public static bool IsNumeric(this string s)
    {
      float output;
      return float.TryParse(s.Trim(), out output);
    }
  }
}
