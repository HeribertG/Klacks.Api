// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IContainerSplitWriter. The second part copies shift, scenario, bases and transport mode of the container. Moved
/// sub-works keep their ids and take the day of the second part; their owner follows ContainerChildOwner (customer of the
/// shift, else the person of the second part). Moved sub-breaks take the person and day of the second part.
/// </summary>
/// <param name="context">Database context the split is staged on</param>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class ContainerSplitWriter : IContainerSplitWriter
{
    private readonly DataBaseContext _context;

    public ContainerSplitWriter(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<Work> StageAsync(ContainerSplitPlan plan, CancellationToken cancellationToken)
    {
        var container = await _context.Work.SingleAsync(w => w.Id == plan.ContainerId && !w.IsDeleted, cancellationToken);
        var originalEnd = container.EndTime;
        container.EndTime = plan.SplitTime;

        var secondPart = new Work
        {
            Id = Guid.NewGuid(),
            ClientId = plan.TargetClientId,
            ShiftId = container.ShiftId,
            CurrentDate = plan.SecondPartDate,
            StartTime = plan.SplitTime,
            EndTime = originalEnd,
            AnalyseToken = container.AnalyseToken,
            StartBase = container.StartBase,
            EndBase = container.EndBase,
            TransportMode = container.TransportMode,
            SplitFromWorkId = container.SplitFromWorkId ?? container.Id,
        };
        _context.Work.Add(secondPart);

        var movedIds = plan.MovedChildIds.ToList();
        var movedWorks = await _context.Work
            .Where(w => w.ParentWorkId == container.Id && !w.IsDeleted && movedIds.Contains(w.Id))
            .ToListAsync(cancellationToken);
        var shiftIds = movedWorks.Select(w => w.ShiftId).Distinct().ToList();
        var shiftClients = await _context.Shift
            .Where(s => shiftIds.Contains(s.Id))
            .Select(s => new { s.Id, s.ClientId })
            .ToDictionaryAsync(s => s.Id, s => s.ClientId, cancellationToken);

        foreach (var work in movedWorks)
        {
            work.ParentWorkId = secondPart.Id;
            work.CurrentDate = plan.SecondPartDate;
            work.ClientId = ContainerChildOwner.Of(shiftClients.GetValueOrDefault(work.ShiftId), plan.TargetClientId);
        }

        var movedBreaks = await _context.Break
            .Where(b => b.ParentWorkId == container.Id && !b.IsDeleted && movedIds.Contains(b.Id))
            .ToListAsync(cancellationToken);
        foreach (var entry in movedBreaks)
        {
            entry.ParentWorkId = secondPart.Id;
            entry.CurrentDate = plan.SecondPartDate;
            ContainerBreakDefaults.Apply(entry, secondPart);
        }

        return secondPart;
    }
}
