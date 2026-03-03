// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string DefaultSoulJson { get; set; } = "[]";

    public string DefaultSkillsJson { get; set; } = "[]";
}
