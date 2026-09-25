// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The steps <see cref="MacroAssignPlanner"/> and <see cref="MacroRevertPlanner"/> share, so a switch and its undo apply the
/// same rules and word their refusals the same way: the refusal for a holder that does not exist, the lookup of a macro
/// reference (Guid.Empty counts as no macro, as in production), the plan warnings of <see cref="MacroAssignmentPolicy"/>
/// including the surcharge warning for absence types, the dry-run scope of a change, the dry-run warnings and the refusal
/// of a macro that cannot run, whose error is cleaned like a name (<see cref="MacroAssignmentNames"/>, longer cap).
/// </summary>
/// <param name="references">Reads macros without tracking</param>
/// <param name="channelInspector">Tells whether a macro outputs surcharges (warning for absence types)</param>
/// <param name="holder">The shift or absence type the plan is about</param>
/// <param name="changes">The references that would be written</param>
/// <param name="dryRun">The dry run over the scope of the plan</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Services.Macros;

namespace Klacks.Api.Application.Services.Macros;

internal static class MacroAssignmentPlanning
{
    private const string ShiftNotFoundMessage = "No shift with id '{0}' exists.";
    private const string AbsenceTypeNotFoundMessage = "No absence type with id '{0}' exists.";
    private const string MacroCannotRunMessage = "The macro '{0}' cannot be used: {1} Nothing was changed.";
    private const string SomeMacroCannotRunMessage = "One of the macros to restore cannot be used: {0} Nothing was changed.";

    public static IReadOnlyList<string> CollectWarnings(
        IMacroOutputChannelInspector channelInspector,
        MacroReferenceHolder holder,
        IReadOnlyList<MacroReferenceChange> changes,
        int membersAlreadyOnTarget)
    {
        var target = changes.Select(change => change.To).FirstOrDefault(macro => macro != null);
        var emitsSurcharges = holder.Target == MacroAssignmentTarget.AbsenceType
            && target != null
            && EmitsSurcharges(channelInspector, target);
        return MacroAssignmentPolicy.CollectWarnings(holder, changes, membersAlreadyOnTarget, emitsSurcharges);
    }

    public static async Task<MacroSnapshot?> FindMacroOrNullAsync(
        IMacroReferenceRepository references, Guid? macroId, CancellationToken cancellationToken)
    {
        var reference = MacroAssignmentPolicy.AsReference(macroId);
        return reference.HasValue ? await references.FindMacroAsync(reference.Value, cancellationToken) : null;
    }

    public static MacroDryRunHolder ToDryRunHolder(MacroReferenceChange change) =>
        new(
            change.Holder.Id,
            MacroAssignmentPolicy.AsReference(change.FromMacroId),
            MacroAssignmentPolicy.AsReference(change.ToMacroId));

    public static IReadOnlyList<string> WithDryRunWarnings(
        IReadOnlyList<string> planWarnings, IReadOnlyCollection<MacroDryRunHolder> holders, MacroDryRunResult dryRun) =>
        [.. planWarnings, .. MacroAssignmentPolicy.CollectDryRunWarnings(holders, dryRun)];

    public static string? DescribeUnusableMacro(IReadOnlyList<MacroReferenceChange> changes, MacroDryRunResult dryRun)
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

    public static string DescribeMissingHolder(MacroAssignmentTarget target, Guid holderId) =>
        Format(target == MacroAssignmentTarget.Shift ? ShiftNotFoundMessage : AbsenceTypeNotFoundMessage, holderId);

    public static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);

    private static bool EmitsSurcharges(IMacroOutputChannelInspector channelInspector, MacroSnapshot macro) =>
        channelInspector.Inspect(macro.Content).LiteralChannels.Any(MacroOutputChannels.Surcharges.Contains);
}
