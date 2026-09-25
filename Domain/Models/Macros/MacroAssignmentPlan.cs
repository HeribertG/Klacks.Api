// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// What a macro switch would do, or why it is refused. For a shift it covers the whole cut group of the addressed shift.
/// </summary>
/// <param name="Holder">The shift or absence type the caller addressed; null when refused</param>
/// <param name="NewMacro">The macro the group would use; null when refused</param>
/// <param name="Changes">The references that would be written, the addressed holder first when it changes</param>
/// <param name="Unchanged">Members of the cut group that already use the new macro (counted, not written)</param>
/// <param name="Warnings">Facts the administrator should know before confirming</param>
/// <param name="Refusal">Why the switch is not possible; null when it is</param>
public record MacroAssignmentPlan(
    MacroReferenceHolder? Holder,
    MacroSnapshot? NewMacro,
    IReadOnlyList<MacroReferenceChange> Changes,
    IReadOnlyList<MacroReferenceHolder> Unchanged,
    IReadOnlyList<string> Warnings,
    string? Refusal)
{
    public static MacroAssignmentPlan Refused(string refusal) =>
        new(
            null,
            null,
            Array.Empty<MacroReferenceChange>(),
            Array.Empty<MacroReferenceHolder>(),
            Array.Empty<string>(),
            refusal);
}
