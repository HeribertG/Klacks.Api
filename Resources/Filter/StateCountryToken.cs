using Klacks_api.Datas;

namespace Klacks_api.Resources.Filter
{
  public class StateCountryToken
  {
    public Guid Id { get; set; }
    public string Country { get; set; } = string.Empty;
    public MultiLanguage CountryName { get; set; } = null!;
    public bool Select { get; set; }
    public string State { get; set; } = string.Empty;
    public MultiLanguage StateName { get; set; } = null!;
  }
}
