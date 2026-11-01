// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reports a work that still stands on an absence as one Error per client and work day, naming the shifts and the
/// absence type. A work whose overlap with the absence is fully handed to substitutes is not reported. The entry
/// replaces the time collision in the display paths only; the booking checks read collisions through
/// ScheduleValidationBuilder.AddCollisions and are not touched. One entry per day, because the error list keeps a
/// single row per client, day and key. With several absence types on the day the entry names the first one.
/// </summary>
/// <param name="entries">The finding list of the check</param>
/// <param name="timeline">Sorted timeline of the client</param>
/// <param name="clientName">Display name of the client</param>
/// <param name="lookup">Replacement windows and names of the check</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class UncoveredAbsenceEntries
{
    public const string ShiftsParamKey = "shifts";

    private const string ShiftSeparator = ", ";

    private static readonly IReadOnlyList<ReplacementInterval> NoReplacements = [];

    public static void Add(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        UncoveredAbsenceLookup lookup)
    {
        var uncovered = new List<(ScheduleBlock Absence, ScheduleBlock Work)>();
        foreach (var pair in timeline.GetCollisions())
        {
            if (AbsenceOverlapDetector.TryGet(pair.A, pair.B, lookup.OnCallBreakIds, out var absence, out var work)
                && AbsenceOverlapDetector.HasUncoveredRest(
                    absence, work, lookup.ReplacementWindows.GetValueOrDefault(work.SourceId, NoReplacements)))
            {
                uncovered.Add((absence, work));
            }
        }

        foreach (var day in uncovered.GroupBy(pair => DateOnly.FromDateTime(pair.Work.CalendarStart)))
        {
            entries.Add(Entry(timeline.ClientId, clientName, day.Key, day.ToList(), lookup));
        }
    }

    private static ScheduleValidationNotificationDto Entry(
        Guid clientId,
        string clientName,
        DateOnly date,
        IReadOnlyList<(ScheduleBlock Absence, ScheduleBlock Work)> pairs,
        UncoveredAbsenceLookup lookup)
    {
        var commentParams = new Dictionary<string, string>
        {
            [ShiftsParamKey] = string.Join(ShiftSeparator, pairs
                .Select(pair => pair.Work.ShiftId)
                .Distinct()
                .Select(shiftId => ShiftLabel(shiftId, lookup))
                .Where(label => label.Length > 0)),
        };
        var absenceName = pairs
            .OrderBy(pair => pair.Absence.Start)
            .ThenBy(pair => pair.Absence.SourceId)
            .Select(pair => lookup.AbsenceNames.GetValueOrDefault(pair.Absence.SourceId))
            .FirstOrDefault(name => name != null);
        LocalizedCommentParams.Add(commentParams, LocalizedCommentParamKeys.Absence, absenceName ?? new MultiLanguage());

        return new ScheduleValidationNotificationDto
        {
            Type = ScheduleValidationType.Error,
            ClientId = clientId,
            ClientName = clientName,
            Date = date,
            Comment = ScheduleValidationKeys.UncoveredAbsence,
            CommentParams = commentParams,
        };
    }

    private static string ShiftLabel(Guid? shiftId, UncoveredAbsenceLookup lookup)
        => shiftId is { } id ? lookup.ShiftLabels.GetValueOrDefault(id, string.Empty) : string.Empty;
}
