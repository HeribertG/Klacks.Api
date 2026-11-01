// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reports a work that still stands on an absence as one Error per client and work day, naming the shifts and the
/// absence type. A work whose overlap with the absence is fully handed to substitutes is not reported. The correction,
/// briefing and travel blocks of a work count with that work and never add a notice of their own. The entry
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

    private sealed record UncoveredPair(ScheduleBlock Absence, Guid WorkId, Guid? ShiftId, DateOnly Date);

    public static void Add(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        UncoveredAbsenceLookup lookup)
    {
        var uncovered = new List<UncoveredPair>();
        var correctionPairs = new List<(ScheduleBlock Absence, ScheduleBlock Correction)>();
        foreach (var pair in timeline.GetCollisions())
        {
            if (!AbsenceOverlapDetector.TryGet(pair.A, pair.B, lookup.OnCallBreakIds, out var absence, out var work))
            {
                continue;
            }

            if (work.BlockType == ScheduleBlockType.Correction)
            {
                correctionPairs.Add((absence, work));
            }
            else if (AbsenceOverlapDetector.HasUncoveredRest(absence, work, WindowsOf(lookup, work.SourceId)))
            {
                uncovered.Add(new UncoveredPair(absence, work.SourceId, work.ShiftId, DayOf(lookup, work)));
            }
        }

        AddCorrectionPairs(uncovered, correctionPairs, lookup);

        foreach (var day in uncovered.GroupBy(pair => pair.Date))
        {
            entries.Add(Entry(timeline.ClientId, clientName, day.Key, day.ToList(), lookup));
        }
    }

    /// <summary>
    /// A correction, briefing or travel block over an absence is judged with its work: nothing extra when the work is
    /// reported already, nothing at all when substitutes hold the whole service time, otherwise the work is reported
    /// for the day and shift it stands on. This includes an absence that touches only the time around the service
    /// time, which no replacement window can cover.
    /// </summary>
    private static void AddCorrectionPairs(
        List<UncoveredPair> uncovered,
        List<(ScheduleBlock Absence, ScheduleBlock Correction)> correctionPairs,
        UncoveredAbsenceLookup lookup)
    {
        foreach (var (absence, correction) in correctionPairs)
        {
            var workId = correction.ParentWorkId!.Value;
            if (uncovered.Any(pair => pair.WorkId == workId))
            {
                continue;
            }

            if (lookup.Works.TryGetValue(workId, out var work))
            {
                if (AbsenceOverlapDetector.IsFullyHandedOver(work.Service.Start, work.Service.End, WindowsOf(lookup, workId)))
                {
                    continue;
                }

                uncovered.Add(new UncoveredPair(absence, workId, work.ShiftId, work.Date));
            }
            else
            {
                uncovered.Add(new UncoveredPair(absence, workId, null, DayOf(lookup, correction)));
            }
        }
    }

    /// <summary>
    /// The day a notice is dated on: the day of the work as entered. A work that starts after midnight once a
    /// replacement took its first hours, or a correction block ahead of the work, would otherwise move it to another day.
    /// </summary>
    private static DateOnly DayOf(UncoveredAbsenceLookup lookup, ScheduleBlock block)
    {
        var workId = block.ParentWorkId ?? block.SourceId;
        return lookup.Works.TryGetValue(workId, out var work) ? work.Date : DateOnly.FromDateTime(block.CalendarStart);
    }

    private static IReadOnlyList<ReplacementInterval> WindowsOf(UncoveredAbsenceLookup lookup, Guid workId)
        => lookup.ReplacementWindows.GetValueOrDefault(workId, NoReplacements);

    private static ScheduleValidationNotificationDto Entry(
        Guid clientId,
        string clientName,
        DateOnly date,
        IReadOnlyList<UncoveredPair> pairs,
        UncoveredAbsenceLookup lookup)
    {
        var commentParams = new Dictionary<string, string>
        {
            [ShiftsParamKey] = string.Join(ShiftSeparator, pairs
                .Select(pair => pair.ShiftId)
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
