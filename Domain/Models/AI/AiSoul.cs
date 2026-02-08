using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.AI;

public class AiSoul : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string? Source { get; set; }
}
