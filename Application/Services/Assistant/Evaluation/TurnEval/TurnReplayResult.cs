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

    /// <summary>
    /// W4 multi-step replay: every tool call the model made across the replay iterations, in order.
    /// Empty for legacy single-shot results — the scorer then falls back to ChosenTool/ToolParameters.
    /// </summary>
    public List<TurnReplayToolCall> ToolCalls { get; set; } = new();

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

/// <summary>
/// One tool call attempt inside a multi-step replay (W4). Name plus the parameters the model sent,
/// so the scorer can credit check-then-act sequences and evaluate slots against the matching call.
/// </summary>
public class TurnReplayToolCall
{
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, object> Parameters { get; set; } = new();
}
