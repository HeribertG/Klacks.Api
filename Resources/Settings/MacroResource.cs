using Klacks.Api.Datas;

namespace Klacks.Api.Resources.Settings
{
  public class MacroResource
  {
    public string Content { get; set; } = string.Empty;

    public MultiLanguage Description { get; set; } = null!;

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }
  }
}
