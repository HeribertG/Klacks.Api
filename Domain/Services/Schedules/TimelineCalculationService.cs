// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Calculates ScheduleBlocks from Work, WorkChange and Break entries.
/// Uses absolute DateTime intervals - no midnight splitting required.
/// SubWorks and SubBreaks (ParentWorkId != null) are filtered out as a defensive
/// guard so the collision pipeline never operates on container children.
/// When ScheduleTimeOptions.DstAware is enabled, intervals are converted from
/// local wall-clock time to UTC using the configured time zone, with explicit
/// handling for invalid (spring-forward) and ambiguous (fall-back) times.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Domain.Services.Schedules;

public class TimelineCalculationService : ITimelineCalculationService
{
    private const string LogPrefix = "[SCHEDULE-DST] ";

    private readonly ScheduleTimeOptions _options;
    private readonly ILogger<TimelineCalculationService> _logger;
    private readonly TimeZoneInfo? _timeZone;

    public TimelineCalculationService(
        IOptions<ScheduleTimeOptions> options,
        ILogger<TimelineCalculationService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.DstAware)
        {
            if (string.IsNullOrWhiteSpace(_options.TimeZoneId))
            {
                _logger.LogError(
                    LogPrefix + "DstAware is enabled but no TimeZoneId is configured - treating DstAware as off (legacy wall-clock semantics)");
            }
            else
            {
                _timeZone = ResolveTimeZone(_options.TimeZoneId);
            }
        }
    }

    public List<ScheduleBlock> CalculateScheduleBlocks(List<Work> works, List<WorkChange> workChanges, List<Break> breaks)
    {
        var topLevelWorks = works.Where(w => w.ParentWorkId == null).ToList();
        var topLevelWorkIds = topLevelWorks.Select(w => w.Id).ToHashSet();
        var topLevelBreaks = breaks.Where(b => b.ParentWorkId == null).ToList();
        var relevantWorkChanges = workChanges.Where(wc => topLevelWorkIds.Contains(wc.WorkId)).ToList();

        var result = new List<ScheduleBlock>();
        var changesByWorkId = relevantWorkChanges.GroupBy(wc => wc.WorkId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var work in topLevelWorks)
        {
            var workStartWall = work.CurrentDate.ToDateTime(work.StartTime);
            var workEndWall = ToEndWall(work.CurrentDate, work.StartTime, work.EndTime);
            var effectiveStartWall = workStartWall;
            var effectiveEndWall = workEndWall;

            if (changesByWorkId.TryGetValue(work.Id, out var changes))
            {
                var beforeChanges = changes
                    .Where(c => c.Type is WorkChangeType.CorrectionStart or WorkChangeType.Briefing or WorkChangeType.TravelStart)
                    .OrderBy(c => GetBeforePriority(c.Type)).ThenBy(c => c.Id)
                    .ToList();

                var afterChanges = changes
                    .Where(c => c.Type is WorkChangeType.CorrectionEnd or WorkChangeType.Debriefing or WorkChangeType.TravelEnd)
                    .OrderBy(c => GetAfterPriority(c.Type)).ThenBy(c => c.Id)
                    .ToList();

                var beforeRunning = TimeSpan.Zero;
                foreach (var change in beforeChanges)
                {
                    var duration = TimeSpan.FromHours((double)change.ChangeTime);
                    var blockEnd = workStartWall - beforeRunning;
                    result.Add(CreateBlockFromWall(change.Id, ScheduleBlockType.Correction,
                        work.ClientId, blockEnd - duration, blockEnd));
                    beforeRunning += duration;
                }

                var afterRunning = TimeSpan.Zero;
                foreach (var change in afterChanges)
                {
                    var duration = TimeSpan.FromHours((double)change.ChangeTime);
                    var blockStart = workEndWall + afterRunning;
                    result.Add(CreateBlockFromWall(change.Id, ScheduleBlockType.Correction,
                        work.ClientId, blockStart, blockStart + duration));
                    afterRunning += duration;
                }

                foreach (var change in changes.Where(c =>
                    c.Type is WorkChangeType.ReplacementStart or WorkChangeType.ReplacementEnd))
                {
                    var duration = TimeSpan.FromHours((double)change.ChangeTime);
                    DateTime replacementStart, replacementEnd;
                    if (change.Type == WorkChangeType.ReplacementStart)
                    {
                        replacementStart = workStartWall;
                        replacementEnd = workStartWall + duration;
                        effectiveStartWall = replacementEnd;
                    }
                    else
                    {
                        replacementEnd = workEndWall;
                        replacementStart = workEndWall - duration;
                        effectiveEndWall = replacementStart;
                    }

                    if (change.ReplaceClientId.HasValue)
                    {
                        result.Add(CreateBlockFromWall(change.Id, ScheduleBlockType.Replacement,
                            change.ReplaceClientId.Value, replacementStart, replacementEnd));
                    }
                }

                foreach (var change in changes.Where(c =>
                    c.Type is WorkChangeType.ReplacementWithin or WorkChangeType.TravelWithin))
                {
                    if (change.Type == WorkChangeType.ReplacementWithin && change.ReplaceClientId.HasValue)
                    {
                        result.Add(CreateBlock(change.Id, ScheduleBlockType.Replacement,
                            change.ReplaceClientId.Value, work.CurrentDate, change.StartTime, change.EndTime));
                    }
                }
            }

            if (effectiveEndWall > effectiveStartWall)
            {
                result.Add(CreateBlockFromWall(work.Id, ScheduleBlockType.Work,
                    work.ClientId, effectiveStartWall, effectiveEndWall, work.ShiftId));
            }
        }

        foreach (var b in topLevelBreaks)
        {
            result.Add(CreateBlock(b.Id, ScheduleBlockType.Break,
                b.ClientId, b.CurrentDate, b.StartTime, b.EndTime));
        }

        return result;
    }

    private static int GetBeforePriority(WorkChangeType type) => type switch
    {
        WorkChangeType.CorrectionStart => 0,
        WorkChangeType.Briefing => 1,
        WorkChangeType.TravelStart => 2,
        _ => 99
    };

    private static int GetAfterPriority(WorkChangeType type) => type switch
    {
        WorkChangeType.CorrectionEnd => 0,
        WorkChangeType.Debriefing => 1,
        WorkChangeType.TravelEnd => 2,
        _ => 99
    };

    /// <summary>
    /// Wall-clock end of an interval given as times on <paramref name="date"/>: an end at or before the start
    /// continues on the next day, so start == end is a 24-hour interval.
    /// </summary>
    private static DateTime ToEndWall(DateOnly date, TimeOnly start, TimeOnly end) =>
        end <= start ? date.AddDays(1).ToDateTime(end) : date.ToDateTime(end);

    private ScheduleBlock CreateBlock(
        Guid sourceId,
        ScheduleBlockType blockType,
        Guid clientId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        Guid? shiftId = null)
    {
        return CreateBlockFromWall(sourceId, blockType, clientId, date.ToDateTime(start), ToEndWall(date, start, end), shiftId);
    }

    /// <summary>
    /// Builds a block from absolute company-local wall-clock instants. Corrections and replacements are computed
    /// from the work's absolute start/end, so a briefing before a 00:30 start lands on the previous evening and a
    /// debriefing after a night shift on the next morning instead of wrapping inside the work date.
    /// </summary>
    private ScheduleBlock CreateBlockFromWall(
        Guid sourceId,
        ScheduleBlockType blockType,
        Guid clientId,
        DateTime startWall,
        DateTime endWall,
        Guid? shiftId = null)
    {
        if (_options.DstAware && _timeZone is not null)
        {
            var startUtc = ConvertWallTimeToUtc(startWall, _timeZone);
            var endUtc = ConvertWallTimeToUtc(endWall, _timeZone);

            if (endUtc <= startUtc)
            {
                endUtc = startUtc.AddTicks(1);
            }

            return new ScheduleBlock(
                sourceId,
                blockType,
                clientId,
                startUtc,
                endUtc,
                shiftId,
                DateTime.SpecifyKind(startWall, DateTimeKind.Unspecified),
                DateTime.SpecifyKind(endWall, DateTimeKind.Unspecified));
        }

        return new ScheduleBlock(
            sourceId,
            blockType,
            clientId,
            DateTime.SpecifyKind(startWall, DateTimeKind.Unspecified),
            DateTime.SpecifyKind(endWall, DateTimeKind.Unspecified),
            shiftId);
    }

    private DateTime ConvertWallTimeToUtc(DateTime wallTime, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(wallTime, DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(unspecified))
        {
            var adjustment = timeZone.GetAdjustmentRules()
                .FirstOrDefault(r => r.DateStart <= unspecified.Date && r.DateEnd >= unspecified.Date);
            var delta = adjustment?.DaylightDelta ?? TimeSpan.FromHours(1);
            var snapped = DateTime.SpecifyKind(unspecified.Add(delta), DateTimeKind.Unspecified);
            _logger.LogWarning(
                LogPrefix + "Invalid local time {Original} in zone {Zone} - snapped to {Snapped}",
                unspecified, timeZone.Id, snapped);
            return TimeZoneInfo.ConvertTimeToUtc(snapped, timeZone);
        }

        if (timeZone.IsAmbiguousTime(unspecified))
        {
            var offsets = timeZone.GetAmbiguousTimeOffsets(unspecified);
            var standardOffset = offsets.OrderBy(o => o).First();
            _logger.LogDebug(
                LogPrefix + "Ambiguous local time {Original} in zone {Zone} - using standard offset {Offset}",
                unspecified, timeZone.Id, standardOffset);
            return new DateTimeOffset(unspecified, standardOffset).UtcDateTime;
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
    }

    private TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (TimeZoneLookup.TryResolve(timeZoneId, out var zone))
        {
            return zone;
        }

        _logger.LogError(
            LogPrefix + "Configured TimeZoneId '{TimeZoneId}' could not be resolved - falling back to UTC",
            timeZoneId);
        return TimeZoneInfo.Utc;
    }
}
