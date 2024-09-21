using Klacks.Api.Datas;

namespace Klacks.Api.Resources.Settings
{
  public class CountryResource
  {
    public string Abbreviation { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public MultiLanguage Name { get; set; } = null!;
    public string Prefix { get; set; } = string.Empty;

    public bool Select { get; set; }
  }
}
