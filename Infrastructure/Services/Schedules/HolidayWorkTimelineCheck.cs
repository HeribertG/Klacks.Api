// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides which days the post-commit timeline checks hand to the holiday-work detector, so the single-day check,
/// the range check and their spill-in handling follow one rule (WorkedCalendarDates): every calendar day a work
/// covers, including the after-midnight part of a night shift and the part of a night shift from the day before
/// the checked range.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class HolidayWorkTimelineCheck
{
    /// <summary>
    /// True when the client works on <paramref name="date"/>. <paramref name="windowTimeline"/> must include the
    /// day before, so a night shift from the previous evening counts for the checked day.
    /// </summary>
    public static bool WorksOnDate(ClientTimeline windowTimeline, DateOnly date) =>
        WorkedCalendarDates.FromBlocks(windowTimeline.Blocks).Contains(date);

    /// <summary>
    /// Every range day the client's work covers from <paramref name="startDate"/> on - including the morning after
    /// the range end (the range check is a full refresh, so such a finding does not linger) - plus the days a night
    /// shift of the day before the range covers.
    /// </summary>
    public static List<DateOnly> RangeCandidates(ClientTimeline timeline, DateOnly startDate, HolidayWorkSpillIn spillIn)
    {
        return WorkedCalendarDates.FromBlocks(timeline.Blocks)
            .Where(d => d >= startDate)
            .Union(spillIn.DatesByClient.GetValueOrDefault(timeline.ClientId, []))
            .Order()
            .ToList();
    }

    /// <summary>
    /// Holiday-work findings for clients who have no work inside the range but whose work of the day before runs
    /// into its first day; clients in <paramref name="checkedClientIds"/> already got those days via RangeCandidates.
    /// </summary>
    public static async Task<List<ScheduleValidationNotificationDto>> EvaluateSpillInOnlyAsync(
        IHolidayWorkEvaluator holidayWorkEvaluator,
        HolidayWorkSpillIn spillIn,
        IReadOnlyCollection<Guid> checkedClientIds,
        CancellationToken cancellationToken)
    {
        var entries = new List<ScheduleValidationNotificationDto>();

        foreach (var (clientId, dates) in spillIn.DatesByClient.Where(x => !checkedClientIds.Contains(x.Key)))
        {
            var clientName = spillIn.ClientNames.GetValueOrDefault(clientId, string.Empty);
            entries.AddRange(await holidayWorkEvaluator.EvaluateAsync(clientId, clientName, dates, cancellationToken));
        }

        return entries;
    }
}
