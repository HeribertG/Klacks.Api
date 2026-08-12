// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillToolsetResult
{
    public List<LLMFunction> Functions { get; init; } = new();

    public bool HasDomainSkillContext { get; init; }

    /// <summary>
    /// Wall-clock milliseconds toolset assembly took for this turn (retrieval being the dominant
    /// part). Carried into the turn's usage row so per-installation diagnostics can separate
    /// pipeline time from model time regardless of which LLM the customer selected.
    /// </summary>
    public long AssemblyMs { get; init; }
}
