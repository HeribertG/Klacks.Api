// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMFunction
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Dictionary<string, object> Parameters { get; set; } = new();

    public List<string> RequiredParameters { get; set; } = new();

    /// <summary>
    /// Why this skill is in the toolset (W1.6). Set by SkillToolsetAssembler and consumed only by
    /// telemetry (trajectory candidates JSON). Never part of the provider payload — every provider
    /// maps LLMFunction explicitly onto its own tool schema.
    /// </summary>
    [JsonIgnore]
    public ToolsetSkillSource? ToolsetSource { get; set; }

    /// <summary>
    /// Rerank score of the knowledge-index retrieval for this skill, null unless
    /// <see cref="ToolsetSource"/> is <see cref="ToolsetSkillSource.Retrieved"/>.
    /// </summary>
    [JsonIgnore]
    public double? RetrievalScore { get; set; }
}
