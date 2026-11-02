// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one field list that keeps the sealed order of a shift in a variant (key) and its plannable copy (OriginalShift)
/// equal. A key PUT writes one of the two rows; Copy carries the mirrored fields onto the other. The fields are split in
/// two groups: the free ones (name, abbreviation, description) never depend on a cut, the cut-relevant ones (dates,
/// times, weekdays, staffing, customer, macro, type) are copied onto the cut pieces when the cuts are made, so a change
/// to them is refused once the family has cuts (FindCutRelevantChanges names them). The cut hierarchy, the status, the
/// analyse token, the scenario and ERP reference fields and the owned collections are not mirrored; the collections are
/// mirrored by the repository from a ShiftCloner copy. Times are compared at minute resolution and WorkTime within one
/// minute, like LockedShiftEditPolicy, because the edit form rounds both on every save.
/// </summary>
/// <param name="source">The row that was written; its mirrored fields are read</param>
/// <param name="target">The other row of the family; its mirrored fields are overwritten</param>
/// <param name="stored">The row as stored before the save</param>
/// <param name="incoming">The row as mapped from the request</param>

using System.Reflection;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class VariantShiftMirror
{
    public static IReadOnlyList<string> CutIndependentFieldNames { get; } =
    [
        nameof(Shift.Name),
        nameof(Shift.Abbreviation),
        nameof(Shift.Description)
    ];

    public static IReadOnlyList<string> CutRelevantFieldNames { get; } =
    [
        nameof(Shift.CuttingAfterMidnight),
        nameof(Shift.MacroId),
        nameof(Shift.AfterShift),
        nameof(Shift.BeforeShift),
        nameof(Shift.EndShift),
        nameof(Shift.FromDate),
        nameof(Shift.StartShift),
        nameof(Shift.UntilDate),
        nameof(Shift.BriefingTime),
        nameof(Shift.DebriefingTime),
        nameof(Shift.TravelTimeAfter),
        nameof(Shift.TravelTimeBefore),
        nameof(Shift.IsFriday),
        nameof(Shift.IsHoliday),
        nameof(Shift.IsMonday),
        nameof(Shift.IsSaturday),
        nameof(Shift.IsSunday),
        nameof(Shift.IsThursday),
        nameof(Shift.IsTuesday),
        nameof(Shift.IsWednesday),
        nameof(Shift.IsWeekdayAndHoliday),
        nameof(Shift.IsSporadic),
        nameof(Shift.SporadicScope),
        nameof(Shift.IsTimeRange),
        nameof(Shift.Quantity),
        nameof(Shift.SumEmployees),
        nameof(Shift.WorkTime),
        nameof(Shift.ShiftType),
        nameof(Shift.ClientId)
    ];

    private static readonly PropertyInfo[] CutIndependentProperties = Resolve(CutIndependentFieldNames);

    private static readonly PropertyInfo[] CutRelevantProperties = Resolve(CutRelevantFieldNames);

    public static IReadOnlyList<string> MirroredFieldNames { get; } = [.. CutIndependentFieldNames, .. CutRelevantFieldNames];

    public static void Copy(Shift source, Shift target)
    {
        foreach (var property in CutIndependentProperties.Concat(CutRelevantProperties))
        {
            property.SetValue(target, property.GetValue(source));
        }
    }

    public static IReadOnlyList<string> FindCutRelevantChanges(Shift stored, Shift incoming)
    {
        return CutRelevantProperties
            .Where(property => Differs(property.GetValue(stored), property.GetValue(incoming)))
            .Select(property => property.Name)
            .ToList();
    }

    private static PropertyInfo[] Resolve(IReadOnlyList<string> names) =>
        names.Select(name => typeof(Shift).GetProperty(name)!).ToArray();

    private static bool Differs(object? stored, object? incoming) => (stored, incoming) switch
    {
        (TimeOnly storedTime, TimeOnly incomingTime) => LockedShiftEditPolicy.TimeDiffers(storedTime, incomingTime),
        (decimal storedAmount, decimal incomingAmount) => LockedShiftEditPolicy.WorkTimeDiffers(storedAmount, incomingAmount),
        _ => !Equals(stored, incoming)
    };
}
