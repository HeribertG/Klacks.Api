// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of one headless turn replay: the first tool call the model chose (or none),
/// diagnostics about deterministic pre-stages that would have hijacked the turn in
/// production, latency and cost. No tool is ever executed and nothing is persisted.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnReplayResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public string? ChosenTool { get; set; }

    public Dictionary<string, object> ToolParameters { get; set; } = new();

    public string Content { get; set; } = string.Empty;

    public long LatencyMs { get; set; }

    public decimal Cost { get; set; }

    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public bool RecipeWouldForce { get; set; }

    public bool EngineRecipeWouldTrigger { get; set; }

    /// <summary>W0.5: name of the operator-authored recipe that would force this turn, null when none.</summary>
    public string? ForcedRecipeName { get; set; }

    /// <summary>W0.5: name of the engine recipe that would trigger on this turn, null when none.</summary>
    public string? TriggeredRecipeName { get; set; }

    public List<string> AvailableToolNames { get; set; } = new();

    public bool ToolChoiceRequired { get; set; }

    public string? ProviderId { get; set; }

    public string? ApiModelId { get; set; }
}
