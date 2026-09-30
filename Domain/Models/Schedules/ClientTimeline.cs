// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Common.RestDays;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Timeline of a client over an arbitrary period.
/// Not day-bound - blocks are kept sorted for efficient queries.
/// </summary>
/// <param name="ClientId">Employee ID</param>
public class ClientTimeline
{
    private const int DaysPerWeek = 7;

    public Guid ClientId { get; }
    public List<ScheduleBlock> Blocks { get; } = [];

    public ClientTimeline(Guid clientId)
    {
        ClientId = clientId;
    }

    public void AddBlock(ScheduleBlock block)
    {
        Blocks.Add(block);
    }

    public void AddBlocks(IEnumerable<ScheduleBlock> blocks)
    {
        Blocks.AddRange(blocks);
    }

    public void SortBlocks()
    {
        Blocks.Sort((a, b) => a.Start.CompareTo(b.Start));
    }

    public List<(ScheduleBlock A, ScheduleBlock B)> GetCollisions()
    {
        var collisions = new List<(ScheduleBlock, ScheduleBlock)>();
        for (var i = 0; i < Blocks.Count; i++)
        {
            for (var j = i + 1; j < Blocks.Count; j++)
            {
                if (Blocks[j].Start >= Blocks[i].End) break;
                if (Blocks[i].SourceId == Blocks[j].SourceId) continue;

                collisions.Add((Blocks[i], Blocks[j]));
            }
        }
        return collisions;
    }

    public List<RestViolation> GetRestViolations(TimeSpan minRest)
    {
        var violations = new List<RestViolation>();
        var workBlocks = Blocks
            .Where(b => b.BlockType == ScheduleBlockType.Work)
            .OrderBy(b => b.Start)
            .ToList();

        for (var i = 0; i < workBlocks.Count - 1; i++)
        {
            var gap = workBlocks[i + 1].Start - workBlocks[i].End;
            if (gap < minRest && gap >= TimeSpan.Zero)
            {
                violations.Add(new RestViolation(
                    workBlocks[i], workBlocks[i + 1], gap, minRest));
            }
        }
        return violations;
    }

    public TimeSpan GetWorkDuration(DateOnly date)
    {
        var total = TimeSpan.Zero;
        foreach (var block in Blocks)
        {
            if (block.BlockType != ScheduleBlockType.Work) continue;
            total += block.GetDurationOnDate(date);
        }
        return total;
    }

    /// <summary>
    /// Total Work duration within the 7-day window starting at <paramref name="weekStart"/>.
    /// Break blocks are excluded — an absence does not count toward the weekly hour cap
    /// (canon: wizard-fixed-cells.md). Cross-midnight shifts are clamped per day, so a night
    /// shift spanning a week boundary contributes its real portion to each week it touches.
    /// </summary>
    /// <param name="weekStart">First day (Monday) of the seven-day window</param>
    public TimeSpan GetWeeklyWorkDuration(DateOnly weekStart)
    {
        var total = TimeSpan.Zero;
        for (var offset = 0; offset < DaysPerWeek; offset++)
        {
            total += GetWorkDuration(weekStart.AddDays(offset));
        }
        return total;
    }

    /// <summary>
    /// Number of rest (non-work) days within the 7-day window starting at <paramref name="weekStart"/>.
    /// Delegates to <see cref="CalendarWeekRestDays"/>, the same rest-day definition every wizard vetoes
    /// on: a day touched only by the morning end of a night is rest only when the next day is free and the
    /// free block reaches the package rest of <paramref name="minimumRestDays"/> (an unknown next start,
    /// i.e. no later block in this timeline, satisfies the free block). A Break-only day is rest. Days are
    /// bucketed on the company-local calendar (<see cref="ScheduleBlock.CalendarStart"/>).
    /// </summary>
    /// <param name="weekStart">First day (Monday) of the seven-day window</param>
    /// <param name="minimumRestDays">Configured MinRestDays; sets the minimum free block</param>
    public int GetRestDayCount(DateOnly weekStart, decimal minimumRestDays)
        => CalendarWeekRestDays.Count(weekStart, WorkIntervals(), minimumRestDays);

    /// <summary>
    /// True when <paramref name="date"/> is a work day under the shared rest-day definition
    /// (<see cref="CalendarWeekRestDays.IsWorkDay"/>), bucketed on the company-local calendar.
    /// </summary>
    /// <param name="date">Calendar day to judge</param>
    /// <param name="minimumRestDays">Configured MinRestDays; sets the minimum free block</param>
    public bool IsWorkDay(DateOnly date, decimal minimumRestDays)
        => CalendarWeekRestDays.IsWorkDay(date, WorkIntervals(), CalendarWeekRestDays.MinimumFreeBlock(minimumRestDays));

    private List<WorkInterval> WorkIntervals()
    {
        var works = new List<WorkInterval>(Blocks.Count);
        foreach (var block in Blocks)
        {
            if (block.BlockType == ScheduleBlockType.Work)
            {
                works.Add(new WorkInterval(block.CalendarStart, block.CalendarEnd));
            }
        }

        return works;
    }

    public bool HasWorkAnchoredOn(DateOnly date)
    {
        foreach (var block in Blocks)
        {
            if (block.BlockType != ScheduleBlockType.Work) continue;
            if (block.OwnerDate == date) return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if a Work block touches <paramref name="date"/> — direct anchor (OwnerDate)
    /// OR cross-midnight extension from the previous day's night shift. Use this for streak /
    /// "consecutive working days" calculations so that a Night shift Apr 28 22:00 → Apr 29 07:00
    /// counts both Apr 28 AND Apr 29 as occupied.
    /// </summary>
    public bool HasWorkOnDay(DateOnly date)
    {
        foreach (var block in Blocks)
        {
            if (block.BlockType != ScheduleBlockType.Work) continue;
            if (block.TouchesDate(date)) return true;
        }
        return false;
    }

    public int GetConsecutiveWorkDays(DateOnly fromDate)
    {
        var count = 0;
        var date = fromDate;
        while (HasWorkAnchoredOn(date))
        {
            count++;
            date = date.AddDays(1);
        }
        return count;
    }

    public int GetConsecutiveWorkDaysBackward(DateOnly fromDate)
    {
        var count = 0;
        var date = fromDate;
        while (HasWorkAnchoredOn(date))
        {
            count++;
            date = date.AddDays(-1);
        }
        return count;
    }

    public List<ScheduleBlock> GetBlocksForDate(DateOnly date)
    {
        return Blocks.Where(b => b.TouchesDate(date)).ToList();
    }

    public bool IsWorking(DateTime point)
    {
        return Blocks.Any(b =>
            b.BlockType == ScheduleBlockType.Work &&
            b.Start <= point && point < b.End);
    }
}
