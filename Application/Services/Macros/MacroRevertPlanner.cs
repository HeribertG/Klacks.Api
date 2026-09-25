// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Plans the undo of a recorded macro switch before anything is written; the confirmation preview, the skills and the
/// revert command handler all use it, so they apply the same rules. An undo is identified by exactly one of switch id,
/// shift id or absence type id (the latter two mean the switch that recorded the latest change of that holder) and covers
/// every row of that switch. It is refused when the switch is itself an undo or was undone already (an undo is final,
/// owner decision F6), and when any row conflicts with the current state — holder gone or no longer switchable, switched
/// again later, changed elsewhere since, or its previous macro deleted; one conflict refuses the whole switch, and the
/// refusal lists the conflicts. The preview adds the dry run backwards over the same scope, refuses a macro that cannot
/// run and adds the dry-run warnings of the policy to the plan. Guid.Empty counts as no macro, as in production. Only the
/// preview is part of <see cref="IMacroRevertPlanner"/>; the plan-only variant is an internal step of it. Nothing is
/// written here. The steps shared with the switch live in <see cref="MacroAssignmentPlanning"/>.
/// </summary>
/// <param name="references">Reads holders and macros without tracking</param>
/// <param name="history">Reads recorded switches</param>
/// <param name="channelInspector">Tells whether a macro outputs surcharges (warning for absence types)</param>
/// <param name="dryRun">Evaluates the undo on real entries for the preview</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Services.Macros;

namespace Klacks.Api.Application.Services.Macros;

public class MacroRevertPlanner : IMacroRevertPlanner
{
    private const int SingleSelector = 1;
    private const int MaxConflictLines = 8;
    private const string LineBreak = "\n";
    private const string ConflictBullet = "- ";
    private const string SelectorMessage =
        "Provide exactly one of switchId, shiftId or absenceTypeId to identify the macro switch to undo.";
    private const string SwitchNotFoundMessage = "No macro switch with id '{0}' is recorded.";
    private const string NoSwitchRecordedMessage =
        "No macro switch made by the assistant is recorded for the {0} with id '{1}'.";
    private const string UndoOfUndoMessage =
        "This switch is itself an undo and cannot be undone; an undo is final, so switch the macro again instead.";
    private const string AlreadyUndoneMessage = "This macro switch was already undone.";
    private const string ConflictsHeadline =
        "The macro switch {0} cannot be undone as a whole, so nothing was changed: {1} of its {2} change(s) conflict with "
        + "the current state. Switch the macro again where needed instead.";
    private const string MoreConflictsLine = "... and {0} more conflict(s).";
    private const string LaterSwitchConflict = "'{0}' was switched again later (switch id {1}); undo that switch first.";
    private const string ChangedElsewhereConflict =
        "The macro of '{0}' was changed outside the assistant since this switch; undoing it would overwrite that change.";
    private const string RestoredMacroDeletedConflict =
        "The macro that '{0}' used before this switch has been deleted since, so it cannot be restored.";

    private readonly IMacroReferenceRepository _references;
    private readonly IMacroAssignmentHistoryRepository _history;
    private readonly IMacroOutputChannelInspector _channelInspector;
    private readonly IMacroDryRunService _dryRun;

    public MacroRevertPlanner(
        IMacroReferenceRepository references,
        IMacroAssignmentHistoryRepository history,
        IMacroOutputChannelInspector channelInspector,
        IMacroDryRunService dryRun)
    {
        _references = references;
        _history = history;
        _channelInspector = channelInspector;
        _dryRun = dryRun;
    }

    internal async Task<MacroRevertPlan> PlanRevertAsync(
        MacroRevertRequest request, CancellationToken cancellationToken = default)
    {
        var (rows, selectedHolderId, selectorRefusal) = await FindSwitchAsync(request, cancellationToken);
        if (selectorRefusal != null)
        {
            return MacroRevertPlan.Refused(selectorRefusal);
        }

        if (rows.Any(row => row.RevertOfHistoryId.HasValue))
        {
            return MacroRevertPlan.Refused(UndoOfUndoMessage);
        }

        if (rows.Any(row => row.RevertedByHistoryId.HasValue))
        {
            return MacroRevertPlan.Refused(AlreadyUndoneMessage);
        }

        var switchId = rows[0].SwitchId;
        var changes = new List<MacroReferenceChange>();
        var conflicts = new List<string>();
        foreach (var row in rows)
        {
            var (change, conflict) = await CheckRowAsync(row, cancellationToken);
            if (conflict != null)
            {
                conflicts.Add(conflict);
            }
            else
            {
                changes.Add(change!);
            }
        }

        if (conflicts.Count > 0)
        {
            return MacroRevertPlan.Refused(DescribeConflicts(switchId, rows.Count, conflicts));
        }

        var holder = changes.FirstOrDefault(change => change.Holder.Id == selectedHolderId)?.Holder ?? changes[0].Holder;
        var warnings = MacroAssignmentPlanning.CollectWarnings(_channelInspector, holder, changes, 0);
        return new MacroRevertPlan(switchId, holder, changes, warnings, null);
    }

    public async Task<MacroRevertPreview> PreviewRevertAsync(
        MacroRevertRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await PlanRevertAsync(request, cancellationToken);
        if (plan.Refusal != null)
        {
            return new MacroRevertPreview(plan, null, plan.Refusal);
        }

        var holders = plan.Changes.Select(MacroAssignmentPlanning.ToDryRunHolder).ToList();
        var dryRun = await _dryRun.RunAsync(plan.Holder!.Target, holders, cancellationToken);
        var warnings = MacroAssignmentPlanning.WithDryRunWarnings(plan.Warnings, holders, dryRun);
        return new MacroRevertPreview(
            plan with { Warnings = warnings }, dryRun, MacroAssignmentPlanning.DescribeUnusableMacro(plan.Changes, dryRun));
    }

    private async Task<(IReadOnlyList<MacroAssignmentHistory> Rows, Guid? SelectedHolderId, string? Refusal)>
        FindSwitchAsync(MacroRevertRequest request, CancellationToken cancellationToken)
    {
        if (request.SelectorCount != SingleSelector)
        {
            return (Array.Empty<MacroAssignmentHistory>(), null, SelectorMessage);
        }

        if (request.SwitchId.HasValue)
        {
            var byId = await _history.GetSwitchAsync(request.SwitchId.Value, cancellationToken);
            if (byId.Count > 0)
            {
                return (byId, null, null);
            }

            return (
                Array.Empty<MacroAssignmentHistory>(),
                null,
                MacroAssignmentPlanning.Format(SwitchNotFoundMessage, request.SwitchId.Value));
        }

        var (target, holderId) = request.ShiftId.HasValue
            ? (MacroAssignmentTarget.Shift, request.ShiftId.Value)
            : (MacroAssignmentTarget.AbsenceType, request.AbsenceTypeId!.Value);
        var latest = await _history.GetLatestAsync(target, holderId, cancellationToken);
        if (latest != null)
        {
            var rows = await _history.GetSwitchAsync(latest.SwitchId, cancellationToken);
            if (rows.Count > 0)
            {
                return (rows, holderId, null);
            }
        }

        return (
            Array.Empty<MacroAssignmentHistory>(),
            null,
            MacroAssignmentPlanning.Format(NoSwitchRecordedMessage, MacroAssignmentPolicy.NounOf(target), holderId));
    }

    private async Task<(MacroReferenceChange? Change, string? Conflict)> CheckRowAsync(
        MacroAssignmentHistory row, CancellationToken cancellationToken)
    {
        var holder = await _references.FindHolderAsync(row.Target, row.TargetId, cancellationToken);
        if (holder == null)
        {
            return (null, MacroAssignmentPlanning.DescribeMissingHolder(row.Target, row.TargetId));
        }

        var holderRefusal = MacroAssignmentPolicy.FindHolderRefusal(holder);
        if (holderRefusal != null)
        {
            return (null, holderRefusal);
        }

        var name = MacroAssignmentNames.Safe(holder.Name);
        var latest = await _history.GetLatestAsync(row.Target, row.TargetId, cancellationToken);
        if (latest != null && latest.SwitchId != row.SwitchId)
        {
            return (null, MacroAssignmentPlanning.Format(LaterSwitchConflict, name, latest.SwitchId));
        }

        if (holder.MacroId != row.NewMacroId)
        {
            return (null, MacroAssignmentPlanning.Format(ChangedElsewhereConflict, name));
        }

        var previousMacroId = MacroAssignmentPolicy.AsReference(row.PreviousMacroId);
        var restored = await MacroAssignmentPlanning.FindMacroOrNullAsync(_references, previousMacroId, cancellationToken);
        if (previousMacroId.HasValue && restored == null)
        {
            return (null, MacroAssignmentPlanning.Format(RestoredMacroDeletedConflict, name));
        }

        var current = await MacroAssignmentPlanning.FindMacroOrNullAsync(_references, row.NewMacroId, cancellationToken);
        return (new MacroReferenceChange(holder, current, restored), null);
    }

    private static string DescribeConflicts(Guid switchId, int rowCount, IReadOnlyList<string> conflicts)
    {
        var lines = new List<string> { MacroAssignmentPlanning.Format(ConflictsHeadline, switchId, conflicts.Count, rowCount) };
        lines.AddRange(conflicts.Take(MaxConflictLines).Select(conflict => ConflictBullet + conflict));
        if (conflicts.Count > MaxConflictLines)
        {
            lines.Add(ConflictBullet + MacroAssignmentPlanning.Format(MoreConflictsLine, conflicts.Count - MaxConflictLines));
        }

        return string.Join(LineBreak, lines);
    }
}
