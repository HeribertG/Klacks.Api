// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the texts of the macro assignment skills: the server-computed preview attached to the confirmation request, and
/// the result of the confirmed switch or undo. A text names the holder, how many other shifts cut from the same order go
/// with it, and the macro before and after ("different macros" when the written references do not share one; Guid.Empty
/// counts as no macro, as in production; names flattened by <see cref="MacroAssignmentNames"/>), the entries in scope and
/// how many of them are sealed, the warnings, the notice that nothing is recalculated, and up to
/// <see cref="MaxSampleLines"/> dry-run samples last (previews only; the result of a confirmed switch or undo repeats the
/// counts, not the sample rows). When the addressed shift already uses the target macro and only other cuts of its order
/// are written, the text says so instead of claiming the shift is switched. It is capped at
/// <see cref="MaxPreviewChars"/> characters, so that together with the confirmation instruction it fits the smallest
/// per-result cap of the tool loop (2000 characters, ContextBudgetPolicy tiny profile); the samples are cut first.
/// <see cref="SaveFailedMessage"/> is the answer when the database rejects the confirmed write; it does not claim that
/// nothing changed, because a failure at commit time leaves that open.
/// </summary>
/// <param name="plan">The planned switch or undo</param>
/// <param name="dryRun">The dry run on real entries (a written switch or undo carries its own)</param>
/// <param name="outcome">The written switch or undo, with the dry run of its preview</param>

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Services.Macros;

namespace Klacks.Api.Application.Skills;

public static class MacroAssignmentTextFormatter
{
    public const int MaxPreviewChars = 1000;
    public const int MaxSampleLines = 5;
    public const int ResultSampleLines = 0;

    public const string SaveFailedMessage =
        "The macro change could not be saved: the database rejected the write (for example because the same data was "
        + "changed at the same time), and it is not certain whether it took effect. Before trying again, look up the "
        + "current macro of the shift or absence type; do not repeat the call blindly.";

    private const string LineBreak = "\n";
    private const string TruncationMarker = " [shortened]";
    private const string ShiftNoun = "shift";
    private const string AbsenceTypeNoun = "absence type";
    private const string NoMacroText = "no macro";
    private const string DeletedMacroText = "a deleted macro";
    private const string DifferentMacrosText = "different macros";
    private const string NoValueText = "none";
    private const string QuotedNameFormat = "'{0}'";
    private const string DateFormat = "yyyy-MM-dd";
    private const string GroupSuffix = " and {0} other cut(s) of its order";
    private const string AssignHeadline = "Switch the calculation macro of the {0} '{1}'{2} from {3} to {4}.";
    private const string AssignOthersHeadline =
        "The {0} '{1}' already uses {2}. Switch the calculation macro of {3} other cut(s) of its order from {4} to {2}.";
    private const string RevertHeadline = "Undo the macro switch {0} on the {1} '{2}'{3}: from {4} back to {5}.";
    private const string AssignedHeadline = "Done: the {0} '{1}'{2} switched from {3} to {4} (switch id {5}).";
    private const string AssignedOthersHeadline =
        "Done: the {0} '{1}' already used {2}; {3} other cut(s) of its order switched from {4} to {2} (switch id {5}).";
    private const string RevertedHeadline = "Undone: the {0} '{1}'{2} went back from {3} to {4} (undo id {5}).";
    private const string AffectedLine = "Entries in scope: {0} ({1} sealed; {2} open).";
    private const string MixedStateNotice =
        "Nothing is recalculated automatically: open entries keep their stored values until edited or recalculated; "
        + "recalculations skip sealed ones, but saving one recalculates it with the switched macro.";
    private const string UndoHint =
        "The whole switch can be undone by its switch id while its references stay unchanged; an undo itself is "
        + "final.";
    private const string SampleHeadline =
        "Dry run, {0} newest open entries: {1} would change (not simulated: overtime stacking, container breaks, "
        + "work changes).";
    private const string SampleLine = "{0}: stored {1}, current {2}, new {3}";
    private const string RecordedSampleLine = "{0}: stored {1}, duration recorded directly, never recalculated";
    private const string NoSampleLine = "No open entry to sample.";
    private const string BudgetLine = "The dry run stopped early (time budget); the sample is incomplete.";

    public static string DescribeAssignPreview(MacroAssignmentPlan plan, MacroDryRunResult dryRun)
    {
        var holder = plan.Holder!;
        var headline = IsSwitched(holder, plan.Changes)
            ? Format(
                AssignHeadline,
                NounOf(holder),
                MacroAssignmentNames.Safe(holder.Name),
                DescribeGroup(holder, plan.Changes),
                DescribeFrom(plan.Changes),
                DescribeTo(plan.Changes))
            : Format(
                AssignOthersHeadline,
                NounOf(holder),
                MacroAssignmentNames.Safe(holder.Name),
                DescribeTo(plan.Changes),
                plan.Changes.Count,
                DescribeFrom(plan.Changes));
        return Compose(headline, plan.Warnings, dryRun, MaxSampleLines, MixedStateNotice);
    }

    public static string DescribeRevertPreview(MacroRevertPlan plan, MacroDryRunResult dryRun)
    {
        var holder = plan.Holder!;
        var headline = Format(
            RevertHeadline,
            plan.SwitchId!.Value,
            NounOf(holder),
            MacroAssignmentNames.Safe(holder.Name),
            DescribeGroup(holder, plan.Changes),
            DescribeFrom(plan.Changes),
            DescribeTo(plan.Changes));
        return Compose(headline, plan.Warnings, dryRun, MaxSampleLines, MixedStateNotice);
    }

    public static string DescribeAssigned(MacroAssignmentOutcome outcome)
    {
        var holder = outcome.Holder;
        var headline = IsSwitched(holder, outcome.Changes)
            ? Format(
                AssignedHeadline,
                NounOf(holder),
                MacroAssignmentNames.Safe(holder.Name),
                DescribeGroup(holder, outcome.Changes),
                DescribeFrom(outcome.Changes),
                DescribeTo(outcome.Changes),
                outcome.SwitchId)
            : Format(
                AssignedOthersHeadline,
                NounOf(holder),
                MacroAssignmentNames.Safe(holder.Name),
                DescribeTo(outcome.Changes),
                outcome.Changes.Count,
                DescribeFrom(outcome.Changes),
                outcome.SwitchId);
        return Compose(headline, outcome.Warnings, outcome.DryRun, ResultSampleLines, MixedStateNotice, UndoHint);
    }

    public static string DescribeReverted(MacroAssignmentOutcome outcome)
    {
        var holder = outcome.Holder;
        var headline = Format(
            RevertedHeadline,
            NounOf(holder),
            MacroAssignmentNames.Safe(holder.Name),
            DescribeGroup(holder, outcome.Changes),
            DescribeFrom(outcome.Changes),
            DescribeTo(outcome.Changes),
            outcome.SwitchId);
        return Compose(headline, outcome.Warnings, outcome.DryRun, ResultSampleLines, MixedStateNotice);
    }

    private static string Compose(
        string headline,
        IReadOnlyList<string> warnings,
        MacroDryRunResult dryRun,
        int sampleLines,
        params string[] notices)
    {
        var lines = new List<string>
        {
            headline,
            Format(AffectedLine, dryRun.TotalEntries, dryRun.SealedEntries, dryRun.OpenEntries)
        };
        lines.AddRange(warnings);
        lines.AddRange(notices);
        lines.AddRange(DescribeSamples(dryRun, sampleLines));
        return Cap(string.Join(LineBreak, lines));
    }

    private static IEnumerable<string> DescribeSamples(MacroDryRunResult dryRun, int sampleLines)
    {
        if (dryRun.Samples.Count > 0)
        {
            yield return Format(SampleHeadline, dryRun.Samples.Count, dryRun.ChangedSamples);
            foreach (var sample in dryRun.Samples.Take(sampleLines))
            {
                yield return DescribeSample(sample);
            }
        }
        else if (!dryRun.BudgetExceeded)
        {
            yield return NoSampleLine;
        }

        if (dryRun.BudgetExceeded)
        {
            yield return BudgetLine;
        }
    }

    private static string DescribeSample(MacroDryRunSample sample)
    {
        var date = sample.Date.ToString(DateFormat, CultureInfo.InvariantCulture);
        return sample.KeepsRecordedValue
            ? Format(RecordedSampleLine, date, Value(sample.StoredValue))
            : Format(SampleLine, date, Value(sample.StoredValue), Value(sample.CurrentValue), Value(sample.NewValue));
    }

    private static bool IsSwitched(MacroReferenceHolder holder, IReadOnlyList<MacroReferenceChange> changes) =>
        changes.Any(change => change.Holder.Id == holder.Id);

    private static string DescribeGroup(MacroReferenceHolder holder, IReadOnlyList<MacroReferenceChange> changes)
    {
        var others = changes.Count(change => change.Holder.Id != holder.Id);
        return others == 0 ? string.Empty : Format(GroupSuffix, others);
    }

    private static string DescribeFrom(IReadOnlyList<MacroReferenceChange> changes) =>
        DescribeMacros(changes.Select(change => (MacroAssignmentPolicy.AsReference(change.FromMacroId), change.From)));

    private static string DescribeTo(IReadOnlyList<MacroReferenceChange> changes) =>
        DescribeMacros(changes.Select(change => (MacroAssignmentPolicy.AsReference(change.ToMacroId), change.To)));

    private static string DescribeMacros(IEnumerable<(Guid? Id, MacroSnapshot? Macro)> macros)
    {
        var distinct = macros.DistinctBy(macro => macro.Id).ToList();
        if (distinct.Count != 1)
        {
            return DifferentMacrosText;
        }

        var (id, macro) = distinct[0];
        if (macro != null)
        {
            return Format(QuotedNameFormat, MacroAssignmentNames.Safe(macro.Name));
        }

        return id.HasValue ? DeletedMacroText : NoMacroText;
    }

    private static string NounOf(MacroReferenceHolder holder) =>
        holder.Target == MacroAssignmentTarget.Shift ? ShiftNoun : AbsenceTypeNoun;

    private static string Value(decimal? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : NoValueText;

    private static string Cap(string text) =>
        text.Length <= MaxPreviewChars ? text : text[..(MaxPreviewChars - TruncationMarker.Length)] + TruncationMarker;

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
