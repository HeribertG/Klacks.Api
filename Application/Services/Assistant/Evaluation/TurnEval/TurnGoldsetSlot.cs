// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Expected tool argument for a goldset turn: which parameter, how to compare it and,
/// for name slots, which entity the argument must resolve to.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnGoldsetSlot
{
    public string Name { get; set; } = string.Empty;

    public SlotMatchMode Match { get; set; } = SlotMatchMode.Exact;

    public string? Value { get; set; }

    public ExpectedEntityRef? Entity { get; set; }
}
