// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Orders the items of a container template after a route optimization like the Ui does: the scheduled visits in
/// route order, each break (sorted by start, stable) inserted before the first item that starts later, so a break
/// ties after an item with the same start. The start of an item is its time range start for a TimeRange visit and its
/// own start otherwise; a missing start counts as midnight.
/// </summary>

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public static class ContainerTemplateItemOrder
{
    /// <summary>
    /// Interleaves breaks with the scheduled visits.
    /// </summary>
    /// <param name="absences">Break items in their template order</param>
    /// <param name="visits">Scheduled visit items in route order</param>
    /// <param name="startMinutes">Start of any item in minutes since midnight (see StartMinutes)</param>
    public static List<T> Interleave<T>(IReadOnlyList<T> absences, IReadOnlyList<T> visits, Func<T, int> startMinutes)
    {
        var result = new List<T>(visits);
        foreach (var absence in absences.OrderBy(startMinutes))
        {
            var absenceStart = startMinutes(absence);
            var insertIndex = result.FindIndex(item => absenceStart < startMinutes(item));
            result.Insert(insertIndex < 0 ? result.Count : insertIndex, absence);
        }

        return result;
    }

    /// <summary>
    /// Start of an item in minutes since midnight.
    /// </summary>
    /// <param name="usesTimeRange">True for a visit whose shift is a TimeRange shift; false for breaks</param>
    public static int StartMinutes(bool usesTimeRange, TimeOnly? startItem, TimeOnly? timeRangeStartItem)
    {
        var start = usesTimeRange ? timeRangeStartItem ?? startItem : startItem;
        return start.HasValue ? ContainerVisitScheduler.ClockMinutes(start.Value) : 0;
    }
}
