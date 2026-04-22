// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Domain.Services.Schedules;

public class TimelineCalculationService : ITimelineCalculationService
{
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
            _timeZone = ResolveTimeZone(_options.TimeZoneId);
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
            var effectiveStart = work.StartTime;
            var effectiveEnd = work.EndTime;

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

                var totalBeforeOffset = TimeSpan.FromHours((double)beforeChanges.Sum(c => c.ChangeTime));
                var totalAfterOffset = TimeSpan.FromHours((double)afterChanges.Sum(c => c.ChangeTime));

                if (totalBeforeOffset > TimeSpan.Zero)
                    effectiveStart = work.StartTime.Add(-totalBeforeOffset);
                if (totalAfterOffset > TimeSpan.Zero)
                    effectiveEnd = work.EndTime.Add(totalAfterOffset);

                var beforeRunning = TimeSpan.Zero;
                foreach (var change in beforeChanges)
                {
                    var duration = TimeSpan.FromHours((double)change.ChangeTime);
                    var blockEnd = work.StartTime.Add(-beforeRunning);
                    var blockStart = blockEnd.Add(-duration);
                    result.Add(CreateBlock(change.Id, ScheduleBlockType.Correction,
                        work.ClientId, work.CurrentDate, blockStart, blockEnd));
                    beforeRunning = beforeRunning.Add(duration);
                }

                var afterRunning = TimeSpan.Zero;
                foreach (var change in afterChanges)
                {
                    var duration = TimeSpan.FromHours((double)change.ChangeTime);
                    var blockStart = work.EndTime.Add(afterRunning);
                    var blockEnd = blockStart.Add(duration);
                    result.Add(CreateBlock(change.Id, ScheduleBlockType.Correction,
                        work.ClientId, work.CurrentDate, blockStart, blockEnd));
                    afterRunning = afterRunning.Add(duration);
                }

                foreach (var change in changes.Where(c =>
                    c.Type is WorkChangeType.ReplacementStart or WorkChangeType.ReplacementEnd))
                {
                    TimeOnly rStart, rEnd;
                    if (change.Type == WorkChangeType.ReplacementStart)
                    {
                        rStart = work.StartTime;
                        rEnd = work.StartTime.Add(TimeSpan.FromHours((double)change.ChangeTime));
                        effectiveStart = rEnd;
                    }
                    else
                    {
                        var dur = TimeSpan.FromHours((double)change.ChangeTime);
                        rEnd = work.EndTime;
                        rStart = work.EndTime.Add(-dur);
                        effectiveEnd = rStart;
                    }

                    if (change.ReplaceClientId.HasValue)
                    {
                        result.Add(CreateBlock(change.Id, ScheduleBlockType.Replacement,
                            change.ReplaceClientId.Value, work.CurrentDate, rStart, rEnd));
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

            result.Add(CreateBlock(work.Id, ScheduleBlockType.Work,
                work.ClientId, work.CurrentDate, effectiveStart, effectiveEnd, work.ShiftId));
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

    private ScheduleBlock CreateBlock(
        Guid sourceId,
        ScheduleBlockType blockType,
        Guid clientId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        Guid? shiftId = null)
    {
        var startWall = date.ToDateTime(start);
        var endWall = end <= start
            ? date.AddDays(1).ToDateTime(end)
            : date.ToDateTime(end);

        if (_options.DstAware && _timeZone is not null)
        {
            var startUtc = ConvertWallTimeToUtc(startWall, _timeZone);
            var endUtc = ConvertWallTimeToUtc(endWall, _timeZone);

            if (endUtc <= startUtc)
            {
                endUtc = startUtc.AddTicks(1);
            }

            return new ScheduleBlock(sourceId, blockType, clientId, startUtc, endUtc, shiftId);
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
                "[SCHEDULE-DST] Invalid local time {Original} in zone {Zone} - snapped to {Snapped}",
                unspecified, timeZone.Id, snapped);
            return TimeZoneInfo.ConvertTimeToUtc(snapped, timeZone);
        }

        if (timeZone.IsAmbiguousTime(unspecified))
        {
            var offsets = timeZone.GetAmbiguousTimeOffsets(unspecified);
            var standardOffset = offsets.OrderBy(o => o).First();
            _logger.LogDebug(
                "[SCHEDULE-DST] Ambiguous local time {Original} in zone {Zone} - using standard offset {Offset}",
                unspecified, timeZone.Id, standardOffset);
            return new DateTimeOffset(unspecified, standardOffset).UtcDateTime;
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
    }

    private TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogError(
                "[SCHEDULE-DST] Configured TimeZoneId '{TimeZoneId}' was not found - falling back to UTC",
                timeZoneId);
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException ex)
        {
            _logger.LogError(ex,
                "[SCHEDULE-DST] Configured TimeZoneId '{TimeZoneId}' is invalid - falling back to UTC",
                timeZoneId);
            return TimeZoneInfo.Utc;
        }
    }
}
