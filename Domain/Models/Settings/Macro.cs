using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Settings;

public class Macro : BaseEntity
{
    public string Content { get; set; } = string.Empty;

    public MultiLanguage Description { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }
}

public enum MacroTypeEnum
{
    DefaultResult = 1,
    Info = 5,
    Filter = 8000
}
