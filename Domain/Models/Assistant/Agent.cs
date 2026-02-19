using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class Agent : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDefault { get; set; }

    public Guid? TemplateId { get; set; }

    public virtual AgentTemplate? Template { get; set; }

    public virtual ICollection<AgentSoulSection> SoulSections { get; set; } = new List<AgentSoulSection>();

    public virtual ICollection<AgentMemory> Memories { get; set; } = new List<AgentMemory>();

    public virtual ICollection<AgentSkill> Skills { get; set; } = new List<AgentSkill>();

    public virtual ICollection<AgentSession> Sessions { get; set; } = new List<AgentSession>();
}
