// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler result of GetTurnOptionsQuery. The outcome stays server-side and separates "no turn of this
/// caller was captured" from "a captured turn offers no option"; the wire payload is the option list
/// alone and cannot express that difference.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Assistant;

public class TurnOptionsResult
{
    public TurnOptionsOutcome Outcome { get; set; } = TurnOptionsOutcome.NotFound;

    public List<TurnOptionDto> Options { get; set; } = new();
}
