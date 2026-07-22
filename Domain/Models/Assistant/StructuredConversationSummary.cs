// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Assistant;

public class StructuredConversationSummary
{
    [JsonPropertyName("openTasks")]
    public List<string> OpenTasks { get; set; } = new();

    [JsonPropertyName("touchedEntities")]
    public List<TouchedEntity> TouchedEntities { get; set; } = new();

    [JsonPropertyName("decisions")]
    public List<string> Decisions { get; set; } = new();

    [JsonPropertyName("facts")]
    public List<string> Facts { get; set; } = new();
}
