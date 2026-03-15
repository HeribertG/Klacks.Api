// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// 2D-Container für alle Clients über einen Zeitraum.
/// Ermöglicht Cross-Client-Analysen wie Abdeckung und Unterbesetzung.
/// </summary>
namespace Klacks.Api.Domain.Models.Schedules;

public class ScheduleBoard
{
    private const int MaxUnderstaffedResults = 100;

    private readonly Dictionary<Guid, ClientTimeline> _timelines = new();

    public IReadOnlyDictionary<Guid, ClientTimeline> Timelines => _timelines;

    public ClientTimeline GetOrCreateTimeline(Guid clientId)
    {
        if (!_timelines.TryGetValue(clientId, out var timeline))
        {
            timeline = new ClientTimeline(clientId);
            _timelines[clientId] = timeline;
        }
        return timeline;
    }

    public void SetTimeline(Guid clientId, ClientTimeline timeline)
    {
        _timelines[clientId] = timeline;
    }

    public void SortAllTimelines()
    {
        foreach (var timeline in _timelines.Values)
        {
            timeline.SortBlocks();
        }
    }

    public List<(ScheduleBlock A, ScheduleBlock B)> GetAllCollisions()
    {
        var collisions = new List<(ScheduleBlock, ScheduleBlock)>();
        foreach (var timeline in _timelines.Values)
        {
            collisions.AddRange(timeline.GetCollisions());
        }
        return collisions;
    }

    public int GetStaffCount(DateTime point)
    {
        var count = 0;
        foreach (var timeline in _timelines.Values)
        {
            if (timeline.IsWorking(point))
            {
                count++;
            }
        }
        return count;
    }

    public List<UnderstaffedPeriod> GetUnderstaffedPeriods(
        DateTime from, DateTime to, int minStaff)
    {
        var events = CollectEvents(from, to);
        var periods = new List<UnderstaffedPeriod>();
        var activeCount = 0;
        DateTime? understaffedSince = null;

        foreach (var evt in events)
        {
            activeCount += evt.IsStart ? 1 : -1;

            if (activeCount < minStaff && understaffedSince == null)
            {
                understaffedSince = evt.Time;
            }
            else if (activeCount >= minStaff && understaffedSince != null)
            {
                periods.Add(new UnderstaffedPeriod(
                    understaffedSince.Value, evt.Time, activeCount, minStaff));
                understaffedSince = null;

                if (periods.Count >= MaxUnderstaffedResults) return periods;
            }
        }

        if (understaffedSince != null)
        {
            periods.Add(new UnderstaffedPeriod(
                understaffedSince.Value, to, activeCount, minStaff));
        }

        return periods;
    }

    public Dictionary<int, int> GetHourlyCoverage(DateOnly date)
    {
        var coverage = new Dictionary<int, int>();
        for (var hour = 0; hour < 24; hour++)
        {
            var point = date.ToDateTime(new TimeOnly(hour, 30));
            coverage[hour] = GetStaffCount(point);
        }
        return coverage;
    }

    public List<RestViolation> GetAllRestViolations(TimeSpan minRest)
    {
        var violations = new List<RestViolation>();
        foreach (var timeline in _timelines.Values)
        {
            violations.AddRange(timeline.GetRestViolations(minRest));
        }
        return violations;
    }

    public List<(Guid ClientId, DateOnly Date, TimeSpan Duration)> GetOvertimeViolations(
        DateOnly startDate, DateOnly endDate, TimeSpan maxDailyWork)
    {
        var violations = new List<(Guid, DateOnly, TimeSpan)>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            foreach (var (clientId, timeline) in _timelines)
            {
                var duration = timeline.GetWorkDuration(date);
                if (duration > maxDailyWork)
                {
                    violations.Add((clientId, date, duration));
                }
            }
        }
        return violations;
    }

    public List<(Guid ClientId, DateOnly StartDate, int ConsecutiveDays)> GetConsecutiveDayViolations(
        DateOnly startDate, DateOnly endDate, int maxConsecutiveDays)
    {
        var violations = new List<(Guid, DateOnly, int)>();
        foreach (var (clientId, timeline) in _timelines)
        {
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var consecutive = timeline.GetConsecutiveWorkDays(date);
                if (consecutive > maxConsecutiveDays)
                {
                    violations.Add((clientId, date, consecutive));
                    date = date.AddDays(consecutive - 1);
                }
            }
        }
        return violations;
    }

    private List<(DateTime Time, bool IsStart)> CollectEvents(DateTime from, DateTime to)
    {
        var events = new List<(DateTime Time, bool IsStart)>();
        foreach (var timeline in _timelines.Values)
        {
            foreach (var block in timeline.Blocks)
            {
                if (block.BlockType != ScheduleBlockType.Work) continue;
                if (block.End <= from || block.Start >= to) continue;

                var start = block.Start < from ? from : block.Start;
                var end = block.End > to ? to : block.End;
                events.Add((start, true));
                events.Add((end, false));
            }
        }
        events.Sort((a, b) =>
        {
            var cmp = a.Time.CompareTo(b.Time);
            if (cmp != 0) return cmp;
            return a.IsStart ? 1 : -1;
        });
        return events;
    }
}
