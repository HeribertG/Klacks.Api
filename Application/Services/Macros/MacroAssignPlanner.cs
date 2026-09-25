// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Plans a macro switch before anything is written; the confirmation preview, the skills and the assign command handler
/// all use it, so they apply the same rules. A switch needs an existing holder that is neither a scenario row nor a sealed
/// order and an existing target macro that passes <see cref="MacroAssignmentPolicy"/>; for a shift it covers the whole cut
/// group (owner decision F2): the addressed shift first, then every other live shift cut from the same order, each written
/// only when its macro changes. The preview adds the dry run over the whole group (members already on the target with
/// current = new), refuses a macro that cannot run and adds the dry-run warnings of the policy to the plan: a current
/// macro that is deleted, does not compile or fails yields no value, and production then keeps the stored value, which the
/// preview names instead of hiding it. Guid.Empty counts as no macro, as in production. Only the preview is part of
/// <see cref="IMacroAssignPlanner"/>; the plan-only variant is an internal step of it. Nothing is written here. The steps
/// shared with the undo live in <see cref="MacroAssignmentPlanning"/>.
/// </summary>
/// <param name="references">Reads holders, cut groups and macros without tracking</param>
/// <param name="channelInspector">Tells whether a macro outputs surcharges (warning for absence types)</param>
/// <param name="dryRun">Evaluates a switch on real entries for the preview</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Services.Macros;

namespace Klacks.Api.Application.Services.Macros;

public class MacroAssignPlanner : IMacroAssignPlanner
{
    private const string MacroNotFoundMessage = "No macro with id '{0}' exists; deleted macros cannot be assigned.";

    private readonly IMacroReferenceRepository _references;
    private readonly IMacroOutputChannelInspector _channelInspector;
    private readonly IMacroDryRunService _dryRun;

    public MacroAssignPlanner(
        IMacroReferenceRepository references,
        IMacroOutputChannelInspector channelInspector,
        IMacroDryRunService dryRun)
    {
        _references = references;
        _channelInspector = channelInspector;
        _dryRun = dryRun;
    }

    internal async Task<MacroAssignmentPlan> PlanAssignAsync(
        MacroAssignmentTarget target, Guid holderId, Guid macroId, CancellationToken cancellationToken = default)
    {
        var holder = await _references.FindHolderAsync(target, holderId, cancellationToken);
        if (holder == null)
        {
            return MacroAssignmentPlan.Refused(MacroAssignmentPlanning.DescribeMissingHolder(target, holderId));
        }

        var holderRefusal = MacroAssignmentPolicy.FindHolderRefusal(holder);
        if (holderRefusal != null)
        {
            return MacroAssignmentPlan.Refused(holderRefusal);
        }

        var newMacro = await _references.FindMacroAsync(macroId, cancellationToken);
        if (newMacro == null)
        {
            return MacroAssignmentPlan.Refused(MacroAssignmentPlanning.Format(MacroNotFoundMessage, macroId));
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
                member,
                await MacroAssignmentPlanning.FindMacroOrNullAsync(_references, member.MacroId, cancellationToken),
                newMacro));
        }

        var unchanged = members.Where(member => member.MacroId == newMacro.Id).ToList();
        var warnings = MacroAssignmentPlanning.CollectWarnings(_channelInspector, holder, changes, unchanged.Count);
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
            .Select(MacroAssignmentPlanning.ToDryRunHolder)
            .Concat(plan.Unchanged.Select(member => new MacroDryRunHolder(member.Id, member.MacroId, member.MacroId)))
            .ToList();
        var dryRun = await _dryRun.RunAsync(target, holders, cancellationToken);
        var warnings = MacroAssignmentPlanning.WithDryRunWarnings(plan.Warnings, holders, dryRun);
        return new MacroAssignmentPreview(
            plan with { Warnings = warnings }, dryRun, MacroAssignmentPlanning.DescribeUnusableMacro(plan.Changes, dryRun));
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
}
