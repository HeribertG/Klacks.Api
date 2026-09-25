// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The rules for switching the macro of a shift (with every other shift cut from the same order) or of an absence type,
/// and the one place that holds their texts. Refused: a holder inside an analysis scenario, a sealed order (it never
/// changes again; any cut of the order is the way in), a switch where every member of the group already uses the target
/// macro, a macro categorised for an absence kind on a shift and the shift category on an absence type. With the
/// templates shipped with Klacks the category rule rarely bites, because only the AllShift template carries a category;
/// region-setup imports may set the others. Warned: an unsealed order (the macro only matters once the order is sealed),
/// a change of the overtime stacking mode on any written shift (StandardAdditive adds overtime on top, every other
/// function lets the higher value win), group members that already use the target (counted, not written), shifts of the
/// group that used different macros before, an undo that puts different macros back, the sealed order row that keeps its
/// own macro (a cut reset clones it), surcharge output of a macro put on an absence type (absence types only use the
/// result channel), a current reference to a macro that no longer exists and an undo that removes the reference (in both
/// cases production keeps the stored values on recalculation). Dry-run warnings name the samples that get no value today
/// or afterwards (production keeps the stored value there), but only for a side that has a macro reference at all, so an
/// ordinary first assignment or an undo back to no macro stays quiet. Guid.Empty counts as no macro, as in production.
/// Every name in a text is flattened by <see cref="MacroAssignmentNames"/>.
/// </summary>
/// <param name="holder">The shift or absence type the caller addressed</param>
/// <param name="members">Every member of the holder's cut group, the holder included (an absence type is alone)</param>
/// <param name="target">The macro the group would use from now on</param>
/// <param name="changes">The references that would be written, one per member whose macro changes</param>
/// <param name="membersAlreadyOnTarget">Members of the group that already use the target macro and are not written</param>
/// <param name="targetEmitsSurcharges">Whether the target macro outputs on a surcharge channel (10-14)</param>
/// <param name="scope">The holders the dry run evaluated, each with its current and its new macro</param>
/// <param name="dryRun">The dry run over that scope</param>

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Services.Macros;

public static class MacroAssignmentPolicy
{
    private const string ScenarioRefusal =
        "'{0}' belongs to an analysis scenario; macros are only switched on the live plan.";
    private const string SealedOrderRefusal =
        "'{0}' is a sealed order, which never changes again. Pass the id of any shift cut from this order instead: the "
        + "macro is then switched on every cut of the order at once.";
    private const string SameMacroRefusal = "'{0}' already uses the macro '{1}'. Nothing to change.";
    private const string SameMacroGroupRefusal =
        "'{0}' and every other shift cut from the same order already use the macro '{1}'. Nothing to change.";
    private const string AbsenceCategoryOnShiftRefusal =
        "The macro '{1}' is categorised for an absence kind and cannot be put on the shift '{0}'.";
    private const string ShiftCategoryOnAbsenceTypeRefusal =
        "The macro '{1}' is the calculation for shifts and cannot be put on the absence type '{0}'.";
    private const string UnsealedOrderWarning =
        "'{0}' is an order that is not sealed yet: the macro takes effect for the plannable shift created when the order "
        + "is sealed.";
    private const string AdditiveStackingWarning =
        "Overtime stacking changes: overtime surcharges are then added on top of the macro surcharges.";
    private const string HighestWinsStackingWarning =
        "Overtime stacking changes: then only the higher of the macro and the overtime surcharges counts.";
    private const string AlreadyOnTargetWarning =
        "{0} shift(s) of the order already use this macro: counted here, not written.";
    private const string MixedPreviousWarning =
        "The shifts of this order did not all use the same macro before; an undo puts each one back on its own.";
    private const string MixedRestoredWarning =
        "The shifts of this switch used different macros before it; each shift gets its own previous macro back.";
    private const string SealedOrderKeepsMacroWarning =
        "The sealed order keeps its own macro; a cut reset copies it into the new plannable shift.";
    private const string AbsenceSurchargeWarning =
        "The macro '{0}' also outputs surcharges (channels 10-14); an absence type only uses its result (channel 1), so "
        + "these surcharges are ignored.";
    private const string DeletedCurrentMacroWarning =
        "{0} of {1} reference(s) point at a deleted macro; until now a recalculation keeps their stored values.";
    private const string ReferenceRemovedWarning =
        "The undo removes the macro from {0} of {1} holder(s); a recalculation then keeps their stored values.";
    private const string NoCurrentValueWarning =
        "{0} of {1} sample(s) get no value from the macro used today (none, deleted or failing); the stored value counts.";
    private const string NoNewValueWarning =
        "{0} of {1} sample(s) get no value from the macro used afterwards (failing on that entry); the stored value counts.";

    private static readonly IReadOnlySet<MacroCategoryEnum> AbsenceCategories = new HashSet<MacroCategoryEnum>
    {
        MacroCategoryEnum.Vacation,
        MacroCategoryEnum.Illness,
        MacroCategoryEnum.Accident,
        MacroCategoryEnum.WorkshopPaid,
        MacroCategoryEnum.WorkshopUnpaid
    };

    public static Guid? AsReference(Guid? macroId) => macroId == Guid.Empty ? null : macroId;

    public static string? FindHolderRefusal(MacroReferenceHolder holder)
    {
        if (holder.IsScenario)
        {
            return Format(ScenarioRefusal, holder.Name);
        }

        return holder.ShiftStatus == ShiftStatus.SealedOrder ? Format(SealedOrderRefusal, holder.Name) : null;
    }

    public static string? FindTargetRefusal(
        MacroReferenceHolder holder, IReadOnlyCollection<MacroReferenceHolder> members, MacroSnapshot target)
    {
        if (members.All(member => member.MacroId == target.Id))
        {
            return Format(members.Count > 1 ? SameMacroGroupRefusal : SameMacroRefusal, holder.Name, target.Name);
        }

        if (holder.Target == MacroAssignmentTarget.Shift && AbsenceCategories.Contains(target.Category))
        {
            return Format(AbsenceCategoryOnShiftRefusal, holder.Name, target.Name);
        }

        return holder.Target == MacroAssignmentTarget.AbsenceType && target.Category == MacroCategoryEnum.Shift
            ? Format(ShiftCategoryOnAbsenceTypeRefusal, holder.Name, target.Name)
            : null;
    }

    public static IReadOnlyList<string> CollectWarnings(
        MacroReferenceHolder holder,
        IReadOnlyList<MacroReferenceChange> changes,
        int membersAlreadyOnTarget,
        bool targetEmitsSurcharges)
    {
        var warnings = new List<string>();
        if (holder.ShiftStatus == ShiftStatus.OriginalOrder)
        {
            warnings.Add(Format(UnsealedOrderWarning, holder.Name));
        }

        if (holder.Target == MacroAssignmentTarget.Shift)
        {
            warnings.AddRange(changes
                .Where(change => IsAdditive(change.From) != IsAdditive(change.To))
                .Select(change => IsAdditive(change.To) ? AdditiveStackingWarning : HighestWinsStackingWarning)
                .Distinct());
            AddGroupWarnings(warnings, holder, changes, membersAlreadyOnTarget);
        }

        var target = changes.Select(change => change.To).FirstOrDefault(macro => macro != null);
        if (holder.Target == MacroAssignmentTarget.AbsenceType && targetEmitsSurcharges && target != null)
        {
            warnings.Add(Format(AbsenceSurchargeWarning, target.Name));
        }

        AddMissingMacroWarnings(warnings, changes);
        return warnings;
    }

    public static IReadOnlyList<string> CollectDryRunWarnings(
        IReadOnlyCollection<MacroDryRunHolder> scope, MacroDryRunResult dryRun)
    {
        var warnings = new List<string>();
        var evaluated = dryRun.Samples.Where(sample => !sample.KeepsRecordedValue).ToList();
        var withoutCurrent = evaluated.Count(sample => sample.CurrentValue == null);
        if (withoutCurrent > 0 && scope.Any(holder => AsReference(holder.CurrentMacroId) != null))
        {
            warnings.Add(Format(NoCurrentValueWarning, withoutCurrent, evaluated.Count));
        }

        var withoutNew = evaluated.Count(sample => sample.NewValue == null);
        if (withoutNew > 0 && scope.Any(holder => AsReference(holder.NewMacroId) != null))
        {
            warnings.Add(Format(NoNewValueWarning, withoutNew, evaluated.Count));
        }

        return warnings;
    }

    private static void AddGroupWarnings(
        List<string> warnings,
        MacroReferenceHolder holder,
        IReadOnlyList<MacroReferenceChange> changes,
        int membersAlreadyOnTarget)
    {
        if (membersAlreadyOnTarget > 0)
        {
            warnings.Add(Format(AlreadyOnTargetWarning, membersAlreadyOnTarget));
        }

        if (changes.Select(change => AsReference(change.FromMacroId)).Distinct().Count() > 1)
        {
            warnings.Add(MixedPreviousWarning);
        }

        if (changes.Select(change => change.ToMacroId).Distinct().Count() > 1)
        {
            warnings.Add(MixedRestoredWarning);
        }

        if (holder.CutGroupId.HasValue)
        {
            warnings.Add(SealedOrderKeepsMacroWarning);
        }
    }

    private static void AddMissingMacroWarnings(List<string> warnings, IReadOnlyList<MacroReferenceChange> changes)
    {
        var deleted = changes.Count(change => AsReference(change.FromMacroId) != null && change.From == null);
        if (deleted > 0)
        {
            warnings.Add(Format(DeletedCurrentMacroWarning, deleted, changes.Count));
        }

        var removed = changes.Count(change => change.To == null);
        if (removed > 0)
        {
            warnings.Add(Format(ReferenceRemovedWarning, removed, changes.Count));
        }
    }

    private static bool IsAdditive(MacroSnapshot? macro) =>
        macro != null && macro.Type == (int)MacroFunctionEnum.StandardAdditive;

    private static string Format(string format, params object[] args) =>
        string.Format(
            CultureInfo.InvariantCulture,
            format,
            args.Select(arg => arg is string text ? MacroAssignmentNames.Safe(text) : arg).ToArray());
}
