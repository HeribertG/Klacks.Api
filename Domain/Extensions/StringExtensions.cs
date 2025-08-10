using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using System.Reflection;

namespace Klacks.Api.Domain.Extensions
{
    public static class StringExtensions
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
