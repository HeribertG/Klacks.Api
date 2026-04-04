// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cascades operations (delete, move, lock-level) from a container work to its children.
/// </summary>
/// <param name="context">The EF Core database context used for direct bulk queries on child entities</param>
/// <param name="logger">Logger for diagnostic output</param>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules;

public class ContainerWorkCascadeService : Domain.Interfaces.Schedules.IContainerWorkCascadeService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ContainerWorkCascadeService> _logger;

    public ContainerWorkCascadeService(
        DataBaseContext context,
        ILogger<ContainerWorkCascadeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task DeleteChildrenAsync(Guid parentWorkId)
    {
        var childWorks = await _context.Work
            .Where(w => w.ParentWorkId == parentWorkId && !w.IsDeleted)
            .ToListAsync();

        var childBreaks = await _context.Break
            .Where(b => b.ParentWorkId == parentWorkId && !b.IsDeleted)
            .ToListAsync();

        foreach (var work in childWorks)
            _context.Remove(work);

        foreach (var breakEntry in childBreaks)
            _context.Remove(breakEntry);

        _logger.LogDebug(
            "Deleted {WorkCount} child works and {BreakCount} child breaks for parentWorkId={ParentWorkId}",
            childWorks.Count, childBreaks.Count, parentWorkId);
    }

    public async Task MoveChildrenAsync(Guid parentWorkId, DateOnly newDate)
    {
        var childWorks = await _context.Work
            .Where(w => w.ParentWorkId == parentWorkId && !w.IsDeleted)
            .ToListAsync();

        var childBreaks = await _context.Break
            .Where(b => b.ParentWorkId == parentWorkId && !b.IsDeleted)
            .ToListAsync();

        foreach (var work in childWorks)
            work.CurrentDate = newDate;

        foreach (var breakEntry in childBreaks)
            breakEntry.CurrentDate = newDate;

        _logger.LogDebug(
            "Moved {WorkCount} child works and {BreakCount} child breaks to date={NewDate} for parentWorkId={ParentWorkId}",
            childWorks.Count, childBreaks.Count, newDate, parentWorkId);
    }

    public async Task UpdateLockLevelAsync(Guid parentWorkId, WorkLockLevel lockLevel, string? sealedBy)
    {
        var childWorks = await _context.Work
            .Where(w => w.ParentWorkId == parentWorkId && !w.IsDeleted)
            .ToListAsync();

        var childBreaks = await _context.Break
            .Where(b => b.ParentWorkId == parentWorkId && !b.IsDeleted)
            .ToListAsync();

        var sealedAt = lockLevel != WorkLockLevel.None ? (DateTime?)DateTime.UtcNow : null;
        var resolvedSealedBy = lockLevel != WorkLockLevel.None ? sealedBy : null;

        foreach (var work in childWorks)
        {
            work.LockLevel = lockLevel;
            work.SealedAt = sealedAt;
            work.SealedBy = resolvedSealedBy;
        }

        foreach (var breakEntry in childBreaks)
        {
            breakEntry.LockLevel = lockLevel;
            breakEntry.SealedAt = sealedAt;
            breakEntry.SealedBy = resolvedSealedBy;
        }

        _logger.LogDebug(
            "Updated lock level to {LockLevel} on {WorkCount} child works and {BreakCount} child breaks for parentWorkId={ParentWorkId}",
            lockLevel, childWorks.Count, childBreaks.Count, parentWorkId);
    }
}
