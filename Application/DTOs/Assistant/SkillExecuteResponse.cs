// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Assistant;

public record SkillExecuteResponse
{
    public required bool Success { get; init; }
    public object? Data { get; init; }
    public string? Message { get; init; }
    public required SkillResultType ResultType { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Server-side taint of the executed skill result (externally authored content). Consumed by the MCP
    /// handler to frame the text result as untrusted; excluded from the JSON wire format.
    /// </summary>
    [JsonIgnore]
    public bool ContainsExternalContent { get; init; }
}
