// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What the turn-intent classifier contributes to one turn's toolset.
/// </summary>
/// <param name="ExplainSkillNames">Retrieved Explain skills guaranteed for a confident Ask (Active mode only)</param>
/// <param name="VetoesForcedToolCall">The detector's mutation reason to force a tool call is lifted for this turn</param>

namespace Klacks.Api.Domain.Models.Assistant.TurnIntent;

public sealed record TurnIntentGuaranteeOutcome(IReadOnlyList<string> ExplainSkillNames, bool VetoesForcedToolCall)
{
    public static TurnIntentGuaranteeOutcome None { get; } = new([], false);
}
