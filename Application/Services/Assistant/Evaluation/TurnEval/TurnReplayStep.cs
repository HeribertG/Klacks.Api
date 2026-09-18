// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnReplayStep
{
    public string? Tool { get; set; }

    public Dictionary<string, object> Parameters { get; set; } = new();

    public string Content { get; set; } = string.Empty;

    public long LatencyMs { get; set; }

    public bool Success { get; set; } = true;

    public string? Error { get; set; }
}
