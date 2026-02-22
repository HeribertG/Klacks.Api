// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentSoulSection : BaseEntity
{
    public Guid AgentId { get; set; }

    public string SectionType { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public int Version { get; set; } = 1;

    public string? Source { get; set; }

    public virtual Agent Agent { get; set; } = null!;

    public virtual ICollection<AgentSoulHistory> History { get; set; } = new List<AgentSoulHistory>();
}
