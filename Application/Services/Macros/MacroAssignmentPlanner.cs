// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Plans a macro switch and the undo of a recorded switch before anything is written; the confirmation preview, the
/// skills and the command handlers all use it, so they apply the same rules. A switch needs an existing holder that is
/// neither a scenario row nor a sealed order and an existing target macro that passes <see cref="MacroAssignmentPolicy"/>;
/// for a shift it covers the whole cut group (owner decision F2): the addressed shift first, then every other live shift
/// cut from the same order, each written only when its macro changes. An undo is identified by exactly one of switch id,
/// shift id or absence type id (the latter two mean the switch that recorded the latest change of that holder) and covers
/// every row of that switch. It is refused when the switch is itself an undo or was undone already (an undo is final,
/// owner decision F6), and when any row conflicts with the current state — holder gone or no longer switchable, switched
/// again later, changed elsewhere since, or its previous macro deleted; one conflict refuses the whole switch, and the
/// refusal lists the conflicts. The preview variants add the dry run over the same scope, refuse a macro that cannot run
/// and add the dry-run warnings of the policy to the plan: a current macro that is deleted, does not compile or fails
/// yields no value, and production then keeps the stored value, which the preview names instead of hiding it. Guid.Empty
/// counts as no macro, as in production. The error of a macro that cannot run is cleaned like a name
/// (<see cref="MacroAssignmentNames"/>, longer cap) before it enters a refusal. Only the preview variants are part of
/// <see cref="IMacroAssignmentPlanner"/>; the plan-only variants are internal steps of them. Nothing is written here.
/// </summary>
/// <param name="references">Reads holders, cut groups and macros without tracking</param>
/// <param name="history">Reads recorded switches</param>
/// <param name="channelInspector">Tells whether a macro outputs surcharges (warning for absence types)</param>
/// <param name="dryRun">Evaluates a switch on real entries for the preview</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Services.Macros;

namespace Klacks.Api.Application.Services.Macros;

public class MacroAssignmentPlanner : IMacroAssignmentPlanner
{
    private const int SingleSelector = 1;
    private const int MaxConflictLines = 8;
    private const string LineBreak = "\n";
    private const string ConflictBullet = "- ";
    private const string ShiftNotFoundMessage = "No shift with id '{0}' exists.";
    private const string AbsenceTypeNotFoundMessage = "No absence type with id '{0}' exists.";
    private const string MacroNotFoundMessage = "No macro with id '{0}' exists; deleted macros cannot be assigned.";
    private const string MacroCannotRunMessage = "The macro '{0}' cannot be used: {1} Nothing was changed.";
    private const string SomeMacroCannotRunMessage = "One of the macros to restore cannot be used: {0} Nothing was changed.";
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
    private const string ShiftNoun = "shift";
    private const string AbsenceTypeNoun = "absence type";

    private readonly IMacroReferenceRepository _references;
    private readonly IMacroAssignmentHistoryRepository _history;
    private readonly IMacroOutputChannelInspector _channelInspector;
    private readonly IMacroDryRunService _dryRun;

    public MacroAssignmentPlanner(
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

    internal async Task<MacroAssignmentPlan> PlanAssignAsync(
        MacroAssignmentTarget target, Guid holderId, Guid macroId, CancellationToken cancellationToken = default)
    {
        var holder = await _references.FindHolderAsync(target, holderId, cancellationToken);
        if (holder == null)
        {
            return MacroAssignmentPlan.Refused(DescribeMissingHolder(target, holderId));
        }

        var holderRefusal = MacroAssignmentPolicy.FindHolderRefusal(holder);
        if (holderRefusal != null)
        {
            return MacroAssignmentPlan.Refused(holderRefusal);
        }

        var newMacro = await _references.FindMacroAsync(macroId, cancellationToken);
        if (newMacro == null)
        {
            return MacroAssignmentPlan.Refused(Format(MacroNotFoundMessage, macroId));
        }

        var members = await FindMembersAsync(holder, cancellationToken);
        var targetRefusal = MacroAssignmentPolicy.FindTargetRefusal(holder, members, newMacro);
        if (targetRefusal != null)
        {
            return MacroAssignmentPlan.Refused(targetRefusal);
        }

        var changes = new List<MacroReferenceChange>();
        foreach (var member in members.Where(member => member.MacroId != newMacro.Id))
        {
            changes.Add(new MacroReferenceChange(
                member, await FindMacroOrNullAsync(member.MacroId, cancellationToken), newMacro));
        }

        var unchanged = members.Where(member => member.MacroId == newMacro.Id).ToList();
        var warnings = CollectWarnings(holder, changes, unchanged.Count);
        return new MacroAssignmentPlan(holder, newMacro, changes, unchanged, warnings, null);
    }

    public async Task<MacroAssignmentPreview> PreviewAssignAsync(
        MacroAssignmentTarget target, Guid holderId, Guid macroId, CancellationToken cancellationToken = default)
    {
        var plan = await PlanAssignAsync(target, holderId, macroId, cancellationToken);
        if (plan.Refusal != null)
        {
            return new MacroAssignmentPreview(plan, null, plan.Refusal);
        }

        var holders = plan.Changes
            .Select(ToDryRunHolder)
            .Concat(plan.Unchanged.Select(member => new MacroDryRunHolder(member.Id, member.MacroId, member.MacroId)))
            .ToList();
        var dryRun = await _dryRun.RunAsync(target, holders, cancellationToken);
        var warnings = WithDryRunWarnings(plan.Warnings, holders, dryRun);
        return new MacroAssignmentPreview(
            plan with { Warnings = warnings }, dryRun, DescribeUnusableMacro(plan.Changes, dryRun));
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
        return new MacroRevertPlan(switchId, holder, changes, CollectWarnings(holder, changes, 0), null);
    }

    public async Task<MacroRevertPreview> PreviewRevertAsync(
        MacroRevertRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await PlanRevertAsync(request, cancellationToken);
        if (plan.Refusal != null)
        {
            return new MacroRevertPreview(plan, null, plan.Refusal);
        }

        var holders = plan.Changes.Select(ToDryRunHolder).ToList();
        var dryRun = await _dryRun.RunAsync(plan.Holder!.Target, holders, cancellationToken);
        var warnings = WithDryRunWarnings(plan.Warnings, holders, dryRun);
        return new MacroRevertPreview(
            plan with { Warnings = warnings }, dryRun, DescribeUnusableMacro(plan.Changes, dryRun));
    }

    private async Task<IReadOnlyList<MacroReferenceHolder>> FindMembersAsync(
        MacroReferenceHolder holder, CancellationToken cancellationToken)
    {
        if (holder.Target != MacroAssignmentTarget.Shift)
        {
            return [holder];
        }

        var group = await _references.FindCutGroupAsync(holder.CutGroupKey, cancellationToken);
        return [holder, .. group.Where(member => member.Id != holder.Id)];
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

            return (Array.Empty<MacroAssignmentHistory>(), null, Format(SwitchNotFoundMessage, request.SwitchId.Value));
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

        return (Array.Empty<MacroAssignmentHistory>(), null, Format(NoSwitchRecordedMessage, NounOf(target), holderId));
    }

    private async Task<(MacroReferenceChange? Change, string? Conflict)> CheckRowAsync(
        MacroAssignmentHistory row, CancellationToken cancellationToken)
    {
        var holder = await _references.FindHolderAsync(row.Target, row.TargetId, cancellationToken);
        if (holder == null)
        {
            return (null, DescribeMissingHolder(row.Target, row.TargetId));
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
            return (null, Format(LaterSwitchConflict, name, latest.SwitchId));
        }

        if (holder.MacroId != row.NewMacroId)
        {
            return (null, Format(ChangedElsewhereConflict, name));
        }

        var previousMacroId = MacroAssignmentPolicy.AsReference(row.PreviousMacroId);
        var restored = await FindMacroOrNullAsync(previousMacroId, cancellationToken);
        if (previousMacroId.HasValue && restored == null)
        {
            return (null, Format(RestoredMacroDeletedConflict, name));
        }

        var current = await FindMacroOrNullAsync(row.NewMacroId, cancellationToken);
        return (new MacroReferenceChange(holder, current, restored), null);
    }

    private IReadOnlyList<string> CollectWarnings(
        MacroReferenceHolder holder, IReadOnlyList<MacroReferenceChange> changes, int membersAlreadyOnTarget)
    {
        var target = changes.Select(change => change.To).FirstOrDefault(macro => macro != null);
        var emitsSurcharges = holder.Target == MacroAssignmentTarget.AbsenceType && target != null && EmitsSurcharges(target);
        return MacroAssignmentPolicy.CollectWarnings(holder, changes, membersAlreadyOnTarget, emitsSurcharges);
    }

    private bool EmitsSurcharges(MacroSnapshot macro) =>
        _channelInspector.Inspect(macro.Content).LiteralChannels.Any(MacroOutputChannels.Surcharges.Contains);

    private async Task<MacroSnapshot?> FindMacroOrNullAsync(Guid? macroId, CancellationToken cancellationToken)
    {
        var reference = MacroAssignmentPolicy.AsReference(macroId);
        return reference.HasValue ? await _references.FindMacroAsync(reference.Value, cancellationToken) : null;
    }

    private static MacroDryRunHolder ToDryRunHolder(MacroReferenceChange change) =>
        new(
            change.Holder.Id,
            MacroAssignmentPolicy.AsReference(change.FromMacroId),
            MacroAssignmentPolicy.AsReference(change.ToMacroId));

    private static IReadOnlyList<string> WithDryRunWarnings(
        IReadOnlyList<string> planWarnings, IReadOnlyCollection<MacroDryRunHolder> holders, MacroDryRunResult dryRun) =>
        [.. planWarnings, .. MacroAssignmentPolicy.CollectDryRunWarnings(holders, dryRun)];

    private static string? DescribeUnusableMacro(IReadOnlyList<MacroReferenceChange> changes, MacroDryRunResult dryRun)
    {
        if (dryRun.NewMacroError == null)
        {
            return null;
        }

        var detail = MacroAssignmentNames.Safe(dryRun.NewMacroError, MacroAssignmentNames.MaxDetailLength);
        var targets = changes.Select(change => change.To).OfType<MacroSnapshot>().DistinctBy(macro => macro.Id).ToList();
        return targets.Count == 1
            ? Format(MacroCannotRunMessage, MacroAssignmentNames.Safe(targets[0].Name), detail)
            : Format(SomeMacroCannotRunMessage, detail);
    }

    private static string DescribeConflicts(Guid switchId, int rowCount, IReadOnlyList<string> conflicts)
    {
        var lines = new List<string> { Format(ConflictsHeadline, switchId, conflicts.Count, rowCount) };
        lines.AddRange(conflicts.Take(MaxConflictLines).Select(conflict => ConflictBullet + conflict));
        if (conflicts.Count > MaxConflictLines)
        {
            lines.Add(ConflictBullet + Format(MoreConflictsLine, conflicts.Count - MaxConflictLines));
        }

        return string.Join(LineBreak, lines);
    }

    private static string DescribeMissingHolder(MacroAssignmentTarget target, Guid holderId) =>
        Format(target == MacroAssignmentTarget.Shift ? ShiftNotFoundMessage : AbsenceTypeNotFoundMessage, holderId);

    private static string NounOf(MacroAssignmentTarget target) =>
        target == MacroAssignmentTarget.Shift ? ShiftNoun : AbsenceTypeNoun;

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
