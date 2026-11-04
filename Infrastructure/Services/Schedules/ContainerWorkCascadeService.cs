// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Cascades operations (delete, restore, move, lock-level) from a container work to its children. The
/// restore brings back only the children the same delete took away: same stamped user and a delete time
/// within the sibling tolerance of the container's, so children removed earlier or by another actor
/// (scenario cleanup, harmonizer) stay deleted. GetChildIdsAsync lists the live children with the same query the delete
/// uses, so a caller can name exactly what a delete would take. GetLiveChildrenAsync reads those children untracked so a
/// handler can check their lock levels before it stages anything: this service does not check them, because a refusal
/// raised after the first staged change could be caught further up and leave the container saved without its children.
/// The lock-level change only raises: a child that is sealed higher than the container keeps its seal.
/// </summary>
/// <param name="context">The EF Core database context used for direct bulk queries on child entities</param>
/// <param name="logger">Logger for diagnostic output</param>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

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
        Guid[] parentWorkIds = [parentWorkId];
        var childWorks = await LiveChildWorks(parentWorkIds).ToListAsync();

        var childBreaks = await LiveChildBreaks(parentWorkIds).ToListAsync();

        foreach (var work in childWorks)
            _context.Remove(work);

        foreach (var breakEntry in childBreaks)
            _context.Remove(breakEntry);

        _logger.LogDebug(
            "Deleted {WorkCount} child works and {BreakCount} child breaks for parentWorkId={ParentWorkId}",
            childWorks.Count, childBreaks.Count, parentWorkId);
    }

    public async Task<ContainerChildIds> GetChildIdsAsync(
        IReadOnlyCollection<Guid> parentWorkIds, CancellationToken cancellationToken = default)
    {
        if (parentWorkIds.Count == 0)
        {
            return ContainerChildIds.None;
        }

        var parentIds = parentWorkIds.ToArray();
        var workIds = await LiveChildWorks(parentIds).Select(w => w.Id).ToListAsync(cancellationToken);
        var breakIds = await LiveChildBreaks(parentIds).Select(b => b.Id).ToListAsync(cancellationToken);
        return new ContainerChildIds(workIds, breakIds);
    }

    public async Task<ContainerChildren> GetLiveChildrenAsync(
        IReadOnlyCollection<Guid> parentWorkIds, CancellationToken cancellationToken = default)
    {
        if (parentWorkIds.Count == 0)
        {
            return ContainerChildren.None;
        }

        var parentIds = parentWorkIds.ToArray();
        var works = await LiveChildWorks(parentIds).AsNoTracking().ToListAsync(cancellationToken);
        var breaks = await LiveChildBreaks(parentIds).AsNoTracking().ToListAsync(cancellationToken);
        return new ContainerChildren(works, breaks);
    }

    private IQueryable<Work> LiveChildWorks(Guid[] parentWorkIds) =>
        _context.Work.Where(w => w.ParentWorkId.HasValue && parentWorkIds.Contains(w.ParentWorkId.Value) && !w.IsDeleted);

    private IQueryable<Break> LiveChildBreaks(Guid[] parentWorkIds) =>
        _context.Break.Where(b => b.ParentWorkId.HasValue && parentWorkIds.Contains(b.ParentWorkId.Value) && !b.IsDeleted);

    public async Task RestoreChildrenAsync(Guid parentWorkId, DateTime deletedTime, string? deletedBy)
    {
        var tolerance = TimeSpan.FromSeconds(WorkRestoreDefaults.SiblingDeleteToleranceSeconds);
        var windowStart = deletedTime - tolerance;
        var windowEnd = deletedTime + tolerance;

        var childWorks = await _context.Work
            .IgnoreQueryFilters()
            .Where(w => w.ParentWorkId == parentWorkId
                && w.IsDeleted
                && w.CurrentUserDeleted == deletedBy
                && w.DeletedTime >= windowStart
                && w.DeletedTime <= windowEnd)
            .ToListAsync();

        var childBreaks = await _context.Break
            .IgnoreQueryFilters()
            .Where(b => b.ParentWorkId == parentWorkId
                && b.IsDeleted
                && b.CurrentUserDeleted == deletedBy
                && b.DeletedTime >= windowStart
                && b.DeletedTime <= windowEnd)
            .ToListAsync();

        foreach (var work in childWorks)
            ClearDeleteStamp(work);

        foreach (var breakEntry in childBreaks)
            ClearDeleteStamp(breakEntry);

        _logger.LogDebug(
            "Restored {WorkCount} child works and {BreakCount} child breaks for parentWorkId={ParentWorkId}",
            childWorks.Count, childBreaks.Count, parentWorkId);
    }

    private static void ClearDeleteStamp(BaseEntity entity)
    {
        entity.IsDeleted = false;
        entity.DeletedTime = null;
        entity.CurrentUserDeleted = null;
    }

    public async Task MoveChildrenAsync(Guid parentWorkId, DateOnly newDate, Guid previousClientId, Guid newClientId)
    {
        var childWorks = await _context.Work
            .Where(w => w.ParentWorkId == parentWorkId && !w.IsDeleted)
            .ToListAsync();

        var childBreaks = await _context.Break
            .Where(b => b.ParentWorkId == parentWorkId && !b.IsDeleted)
            .ToListAsync();

        foreach (var work in childWorks)
        {
            work.CurrentDate = newDate;
            if (work.ClientId == previousClientId)
            {
                work.ClientId = newClientId;
            }
        }

        foreach (var breakEntry in childBreaks)
        {
            breakEntry.CurrentDate = newDate;
            breakEntry.ClientId = newClientId;
        }

        _logger.LogDebug(
            "Moved {WorkCount} child works and {BreakCount} child breaks to date={NewDate}, clientId={NewClientId} for parentWorkId={ParentWorkId}",
            childWorks.Count, childBreaks.Count, newDate, newClientId, parentWorkId);
    }

    public async Task UpdateLockLevelAsync(Guid parentWorkId, WorkLockLevel lockLevel, string? sealedBy)
    {
        var childWorks = await _context.Work
            .Where(w => w.ParentWorkId == parentWorkId && !w.IsDeleted)
            .ToListAsync();

        var childBreaks = await _context.Break
            .Where(b => b.ParentWorkId == parentWorkId && !b.IsDeleted)
            .ToListAsync();

        var sealedAt = DateTime.UtcNow;

        foreach (var work in childWorks)
        {
            ApplyLockLevel(work, lockLevel, sealedAt, sealedBy);
        }

        foreach (var breakEntry in childBreaks)
        {
            ApplyLockLevel(breakEntry, lockLevel, sealedAt, sealedBy);
        }

        _logger.LogDebug(
            "Updated lock level to {LockLevel} on {WorkCount} child works and {BreakCount} child breaks for parentWorkId={ParentWorkId}",
            lockLevel, childWorks.Count, childBreaks.Count, parentWorkId);
    }

    public async Task ReleaseLockLevelAsync(Guid parentWorkId, WorkLockLevel previousLockLevel)
    {
        var childWorks = await _context.Work
            .Where(w => w.ParentWorkId == parentWorkId && !w.IsDeleted
                && w.LockLevel != WorkLockLevel.None && w.LockLevel <= previousLockLevel)
            .ToListAsync();

        var childBreaks = await _context.Break
            .Where(b => b.ParentWorkId == parentWorkId && !b.IsDeleted
                && b.LockLevel != WorkLockLevel.None && b.LockLevel <= previousLockLevel)
            .ToListAsync();

        foreach (var entry in childWorks.Cast<ScheduleEntryBase>().Concat(childBreaks))
        {
            entry.LockLevel = WorkLockLevel.None;
            entry.SealedAt = null;
            entry.SealedBy = null;
            entry.PreSealLockLevel = null;
            entry.PreSealSealedAt = null;
            entry.PreSealSealedBy = null;
        }
    }

    /// <summary>
    /// Mirrors the parent's single-entry lock change onto a child, but only upwards: a child sealed as high as the new level or
    /// higher keeps its seal and its pre-seal state. Like WorkLockLevelService it drops the pre-seal state a period seal
    /// recorded when it raises the child, because that state no longer describes the entry.
    /// </summary>
    private static void ApplyLockLevel(ScheduleEntryBase entry, WorkLockLevel lockLevel, DateTime sealedAt, string? sealedBy)
    {
        if (lockLevel <= entry.LockLevel)
        {
            return;
        }

        entry.LockLevel = lockLevel;
        entry.SealedAt = sealedAt;
        entry.SealedBy = sealedBy;
        entry.PreSealLockLevel = null;
        entry.PreSealSealedAt = null;
        entry.PreSealSealedBy = null;
    }
}
