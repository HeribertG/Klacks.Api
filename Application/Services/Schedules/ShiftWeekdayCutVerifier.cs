// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Re-reads the result of a weekday cut straight from the database (untracked) and lists what does not match the plan: every
/// planned part must exist as a SplitShift of the sealed order, a root of its own (no parent, root = itself), with the
/// customer, macro, type, dates, times, staff count, quantity, time-range and sporadic flags of the plannable shift, exactly
/// its one weekday flag, and the same group links (and their validity) as the plannable shift; no plannable shift of a cut
/// order may be left over. An empty list means the cut is confirmed.
/// </summary>
/// <param name="shiftReader">Reads the parts, the left-over plannable shifts and the group links</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ShiftWeekdayCutVerifier
{
    private readonly IShiftReader _shiftReader;

    public ShiftWeekdayCutVerifier(IShiftReader shiftReader)
    {
        _shiftReader = shiftReader;
    }

    /// <param name="plans">The cuts that were processed</param>
    /// <param name="sourceLinks">The group links of the plannable shifts before the cut, keyed by shift id</param>
    /// <param name="cancellationToken">Cancels the reads</param>
    public async Task<List<string>> FindMismatchesAsync(
        IReadOnlyList<ShiftWeekdayCutPlan> plans,
        IReadOnlyDictionary<Guid, IReadOnlyList<ShiftGroupLink>> sourceLinks,
        CancellationToken cancellationToken)
    {
        var partIds = plans.SelectMany(plan => plan.Parts.Select(part => part.Id)).ToList();
        var sourceIds = plans.Select(plan => plan.Source.Id).ToList();

        var storedParts = (await _shiftReader.ScanRealPlanAsync(
                new RealPlanShiftScan(ShiftStatus.SplitShift) { OnlyIds = partIds }, cancellationToken))
            .ToDictionary(shift => shift.Id);
        var leftOver = await _shiftReader.ScanRealPlanAsync(
            new RealPlanShiftScan(ShiftStatus.OriginalShift) { OnlyIds = sourceIds }, cancellationToken);
        var storedLinks = (await _shiftReader.GetGroupLinksByShiftIdsAsync(partIds, cancellationToken))
            .ToLookup(link => link.ShiftId);

        var mismatches = leftOver
            .Select(shift => $"'{shift.Name}' is still a plannable shift after the cut.")
            .ToList();

        foreach (var plan in plans)
        {
            foreach (var part in plan.Parts)
            {
                mismatches.AddRange(storedParts.TryGetValue(part.Id, out var stored)
                    ? Compare(plan, part, stored, sourceLinks[plan.Source.Id], storedLinks[part.Id].ToList())
                    : [$"The part for weekday {part.IsoWeekday} of '{plan.Source.Name}' is missing."]);
            }
        }

        return mismatches;
    }

    private static List<string> Compare(
        ShiftWeekdayCutPlan plan,
        ShiftWeekdayCutPart part,
        Shift stored,
        IReadOnlyList<ShiftGroupLink> expectedLinks,
        IReadOnlyList<ShiftGroupLink> storedLinks)
    {
        var source = plan.Source;
        var problems = new List<string>();
        var label = $"The part for weekday {part.IsoWeekday} of '{source.Name}'";

        Check(problems, label, "origin", stored.OriginalId == plan.SealedOrderId);
        Check(problems, label, "root position", stored.ParentId == null && stored.RootId == stored.Id);
        Check(problems, label, "customer", stored.ClientId == source.ClientId);
        Check(problems, label, "macro", stored.MacroId == source.MacroId);
        Check(problems, label, "type", stored.ShiftType == source.ShiftType);
        Check(problems, label, "time-range flag", stored.IsTimeRange == source.IsTimeRange);
        Check(problems, label, "sporadic flag", stored.IsSporadic == source.IsSporadic);
        Check(problems, label, "staff count and quantity", stored.SumEmployees == source.SumEmployees && stored.Quantity == source.Quantity);
        Check(problems, label, "dates", stored.FromDate == source.FromDate && stored.UntilDate == source.UntilDate);
        Check(problems, label, "times", stored.StartShift == source.StartShift && stored.EndShift == source.EndShift && stored.WorkTime == source.WorkTime);
        Check(problems, label, "weekday", WeekdayFlags.Of(stored).IsoDays().SequenceEqual([part.IsoWeekday]));
        Check(problems, label, "groups", GroupLinksMatch(expectedLinks, storedLinks));
        return problems;
    }

    private static bool GroupLinksMatch(IReadOnlyList<ShiftGroupLink> expected, IReadOnlyList<ShiftGroupLink> stored) =>
        expected.Select(link => link.GroupId).Order().SequenceEqual(stored.Select(link => link.GroupId).Order())
        && expected.All(link => link.ValidFrom == null
            || stored.Any(candidate => candidate.GroupId == link.GroupId && candidate.ValidFrom == link.ValidFrom));

    private static void Check(List<string> problems, string label, string what, bool matches)
    {
        if (!matches)
        {
            problems.Add($"{label} does not carry the {what} of the plannable shift.");
        }
    }
}
