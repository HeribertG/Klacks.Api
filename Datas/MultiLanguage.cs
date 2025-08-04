using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Datas;

[Owned]
public class MultiLanguage
{
    public string De { get; set; } = string.Empty;

    public string? En { get; set; }

    public string? Fr { get; set; }

    public string? It { get; set; }

    public Dictionary<string, string> ToDictionary()
    {
        var result = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(De))
        {
            result["de"] = De;
        }

        if (!string.IsNullOrEmpty(En))
        {
            result["en"] = En;
        }

        if (!string.IsNullOrEmpty(Fr))
        {
            result["fr"] = Fr;
        }

        if (!string.IsNullOrEmpty(It))
        {
            result["it"] = It;
        }

        return result;
    }

    public bool IsEmpty => string.IsNullOrEmpty(De) &&
                          string.IsNullOrEmpty(En) &&
                          string.IsNullOrEmpty(Fr) &&
                          string.IsNullOrEmpty(It);

    public static MultiLanguage Empty() => new() { De = string.Empty };
}
