using Klacks_api.Datas;

namespace Klacks_api.Resources.Settings
{
  public class StateResource
  {
    public string Abbreviation { get; set; } = string.Empty;
    public string CountryPrefix { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public MultiLanguage Name { get; set; } = null!;
  }
}
