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

    /// <summary>
    /// Free-form subset markers (e.g. "crud") so an eval run can select a named slice of a
    /// goldset file by tag instead of needing a separate goldset file per subset.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    public string? Source { get; set; }

    public string? Comment { get; set; }

    public TurnGoldsetHonesty? Honesty { get; set; }

    /// <summary>
    /// TP1: the assistant turn this item corrects. Null for an ordinary item, which is then never
    /// measured on any correction dimension.
    /// </summary>
    public TurnGoldsetPreviousTurn? PreviousTurn { get; set; }

    /// <summary>
    /// True when the graceful-correction path MUST engage on this item. False together with a
    /// PreviousTurn marks the counter-example: an ordinary follow-up that must NOT be repaired.
    /// </summary>
    public bool ExpectsCorrection { get; set; }

    /// <summary>
    /// True when the correct outcome is the deterministic two-option clarification rather than a
    /// re-routed tool call. Such an item declares no ExpectedTool.
    /// </summary>
    public bool ExpectsClarification { get; set; }

    /// <summary>
    /// Inverse skill the turn is expected to offer as an undo, null when no undo is expected.
    /// </summary>
    public string? ExpectedUndoSkill { get; set; }
}
