// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillToolsetResult
{
    public List<LLMFunction> Functions { get; init; } = new();

    public bool HasDomainSkillContext { get; init; }
}
