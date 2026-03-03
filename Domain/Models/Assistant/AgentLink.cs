// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentLink : BaseEntity
{
    public Guid SourceAgentId { get; set; }

    public Guid TargetAgentId { get; set; }

    public string LinkType { get; set; } = string.Empty;

    public string Config { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public virtual Agent SourceAgent { get; set; } = null!;

    public virtual Agent TargetAgent { get; set; } = null!;
}
