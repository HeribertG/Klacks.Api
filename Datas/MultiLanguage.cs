using Microsoft.EntityFrameworkCore;

namespace Klacks_api.Datas;

[Owned]
public class MultiLanguage
{
  public string? De { get; set; } = null!;

  public string? En { get; set; } = null!;

  public string? Fr { get; set; } = null!;

  public string? It { get; set; } = null!;


  public Dictionary<string, string> ToDictionary()
  {
    var result = new Dictionary<string, string>();
    if (!string.IsNullOrEmpty(De)) result["de"] = De;
    if (!string.IsNullOrEmpty(En)) result["en"] = En;
    if (!string.IsNullOrEmpty(Fr)) result["fr"] = Fr;
    if (!string.IsNullOrEmpty(It)) result["it"] = It;

    return result;
  }
}
