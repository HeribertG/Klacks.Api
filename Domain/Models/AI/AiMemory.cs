using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.AI;

public class AiMemory : BaseEntity
{
    public string Category { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int Importance { get; set; } = 5;

    public string? Source { get; set; }
}
