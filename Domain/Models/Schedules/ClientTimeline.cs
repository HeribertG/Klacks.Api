// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Zeitstrahl eines Clients über einen beliebigen Zeitraum.
/// Nicht tagesgebunden — Blöcke werden sortiert gehalten für effiziente Abfragen.
/// </summary>
/// <param name="ClientId">Mitarbeiter-ID</param>
namespace Klacks.Api.Domain.Models.Schedules;

public class ClientTimeline
{
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

    public int GetConsecutiveWorkDays(DateOnly fromDate)
    {
        var count = 0;
        var date = fromDate;
        while (GetWorkDuration(date) > TimeSpan.Zero)
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
        while (GetWorkDuration(date) > TimeSpan.Zero)
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
