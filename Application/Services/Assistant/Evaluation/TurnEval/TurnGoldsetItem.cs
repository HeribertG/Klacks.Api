// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One evaluated chat turn: the user message plus the tool call (name and arguments)
/// the model is expected to produce. ExpectedTool null marks a pure-conversation turn
/// where no tool call at all is the correct behaviour.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnGoldsetItem
{
    public string Id { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Locale { get; set; }

    public string? CurrentRoute { get; set; }

    public string? ExpectedTool { get; set; }

    /// <summary>
    /// W0.5: when set, the item does not expect a specific tool call but the deterministic engagement of
    /// this recipe (operator-authored or engine recipe). The scorer then measures "did the expected
    /// recipe trigger" instead of excluding the turn from the tool dimension.
    /// </summary>
    public string? ExpectedRecipe { get; set; }

    public List<string> AlternativeTools { get; set; } = new();

    public List<TurnGoldsetSlot> ExpectedSlots { get; set; } = new();

    public string? Source { get; set; }

    public string? Comment { get; set; }

    public TurnGoldsetHonesty? Honesty { get; set; }
}
