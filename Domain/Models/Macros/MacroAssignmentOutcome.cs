// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// A written macro switch or undo.
/// </summary>
/// <param name="SwitchId">Id shared by every history row of this switch or undo; the id the assistant reports</param>
/// <param name="Holder">The shift or absence type the texts are named after, as it was before the write</param>
/// <param name="Changes">The references that were written, one per holder</param>
/// <param name="Warnings">The warnings of the preview the write followed, the dry-run warnings included</param>
/// <param name="DryRun">The dry run of the preview the write followed, so the answer repeats its counts without running
/// it a second time</param>
/// <param name="UndoneSwitchId">For an undo: the id of the switch it undid; null for a switch</param>
public record MacroAssignmentOutcome(
    Guid SwitchId,
    MacroReferenceHolder Holder,
    IReadOnlyList<MacroReferenceChange> Changes,
    IReadOnlyList<string> Warnings,
    MacroDryRunResult DryRun,
    Guid? UndoneSwitchId);
