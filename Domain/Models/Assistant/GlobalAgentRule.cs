// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class GlobalAgentRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public int Version { get; set; } = 1;

    public string? Source { get; set; }

    public virtual ICollection<GlobalAgentRuleHistory> History { get; set; } = new List<GlobalAgentRuleHistory>();
}
