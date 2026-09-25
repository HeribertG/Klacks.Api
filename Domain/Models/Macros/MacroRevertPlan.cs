// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// What undoing a recorded macro switch would do, or why it is refused.
/// </summary>
/// <param name="SwitchId">Id of the switch that would be undone; null when refused</param>
/// <param name="Holder">The shift or absence type the texts are named after: the one the caller named, otherwise the first
/// of the switch; null when refused</param>
/// <param name="Changes">One change per row of the switch: from the macro it put there back to each holder's previous one</param>
/// <param name="Warnings">Facts the administrator should know before confirming</param>
/// <param name="Refusal">Why the undo is not possible (with the list of conflicts); null when it is</param>
public record MacroRevertPlan(
    Guid? SwitchId,
    MacroReferenceHolder? Holder,
    IReadOnlyList<MacroReferenceChange> Changes,
    IReadOnlyList<string> Warnings,
    string? Refusal)
{
    public static MacroRevertPlan Refused(string refusal) =>
        new(null, null, Array.Empty<MacroReferenceChange>(), Array.Empty<string>(), refusal);
}
