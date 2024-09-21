using Klacks.Api.Datas;

namespace Klacks.Api.Models.Settings
{
  public class Macro : BaseEntity
  {
    public string Content { get; set; } = string.Empty;

    public MultiLanguage Description { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }
  }
}
