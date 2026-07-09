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

    public List<string> AlternativeTools { get; set; } = new();

    public List<TurnGoldsetSlot> ExpectedSlots { get; set; } = new();

    public string? Source { get; set; }

    public string? Comment { get; set; }
}
