// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core implementation of IEscalationChainRepository. Every transition is an ExecuteUpdateAsync
/// scoped by the expected prior status, mirroring ScheduledTaskRepository.TryClaimAsync: the row
/// only moves for the caller that observes the old status, so two sweep instances (this service is
/// explicitly allowed to run on all of them, see the Entwurf §5) or a sweep racing an incoming reply
/// resolve to exactly one winner without a distributed lock.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Escalation;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class EscalationChainRepository : IEscalationChainRepository
{
    private readonly DataBaseContext _context;

    public EscalationChainRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<bool> AddAsync(EscalationChain chain, CancellationToken cancellationToken = default)
    {
        await _context.Set<EscalationChain>().AddAsync(chain, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The partial unique index on (WorkId) where Status=Running caught a second chain for a
            // shift that already has one running (e.g. CoverAbsence re-run on the same shift). Detach
            // so the failed insert does not poison the next SaveChangesAsync on this context instance.
            _context.Entry(chain).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<EscalationChain?> GetByIdWithStagesAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EscalationChain>()
            .Include(c => c.Stages)
            .FirstOrDefaultAsync(c => c.Id == chainId, cancellationToken);
    }

    public async Task<IReadOnlyList<EscalationStage>> GetStagesByChainAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EscalationStage>()
            .Where(s => s.EscalationChainId == chainId)
            .OrderBy(s => s.Rank)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EscalationStage>> GetDueStagesAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EscalationStage>()
            .Where(s => s.Status == EscalationStageStatus.Notified && s.DueAtUtc != null && s.DueAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetOverdueRunningChainIdsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EscalationChain>()
            .Where(c => c.Status == EscalationChainStatus.Running && c.DeadlineUtc <= nowUtc)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EscalationChain>> GetRunningChainsWithAbsenceBreakAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<EscalationChain>()
            .Where(c => c.Status == EscalationChainStatus.Running && c.AbsenceBreakId != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EscalationChain>> GetRunningChainsWithStagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<EscalationChain>()
            .Include(c => c.Stages)
            .Where(c => c.Status == EscalationChainStatus.Running)
            .OrderBy(c => c.DeadlineUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsBreakDeletedAsync(Guid breakId, CancellationToken cancellationToken = default)
    {
        var breakRow = await _context.Set<Break>()
            .IgnoreQueryFilters()
            .Where(b => b.Id == breakId)
            .Select(b => new { b.IsDeleted })
            .FirstOrDefaultAsync(cancellationToken);

        return breakRow is null || breakRow.IsDeleted;
    }

    public async Task<EscalationStage?> FindNotifiedStageForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EscalationStage>()
            .Include(s => s.Chain)
            .Where(s => s.UserId == userId && s.Status == EscalationStageStatus.Notified)
            .OrderByDescending(s => s.NotifiedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryNotifyStageAsync(
        Guid stageId,
        DateTime notifiedAtUtc,
        DateTime dueAtUtc,
        string? deliveryChannel,
        string? deliveryOutcome,
        Guid? dispatchRowId,
        CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<EscalationStage>()
            .Where(s => s.Id == stageId && s.Status == EscalationStageStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, EscalationStageStatus.Notified)
                .SetProperty(s => s.NotifiedAtUtc, notifiedAtUtc)
                .SetProperty(s => s.DueAtUtc, dueAtUtc)
                .SetProperty(s => s.DeliveryChannel, deliveryChannel)
                .SetProperty(s => s.DeliveryOutcome, deliveryOutcome)
                .SetProperty(s => s.DispatchRowId, dispatchRowId),
                cancellationToken);

        return affected > 0;
    }

    public async Task<bool> TrySkipStageAsync(Guid stageId, string skipReason, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<EscalationStage>()
            .Where(s => s.Id == stageId && s.Status == EscalationStageStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, EscalationStageStatus.Skipped)
                .SetProperty(s => s.SkipReason, skipReason),
                cancellationToken);

        return affected > 0;
    }

    public async Task<bool> TryExpireStageAsync(Guid stageId, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<EscalationStage>()
            .Where(s => s.Id == stageId && s.Status == EscalationStageStatus.Notified)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, EscalationStageStatus.Expired),
                cancellationToken);

        return affected > 0;
    }

    public async Task<bool> TryAcknowledgeStageAsync(Guid stageId, DateTime respondedAtUtc, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<EscalationStage>()
            .Where(s => s.Id == stageId && s.Status == EscalationStageStatus.Notified)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, EscalationStageStatus.Acknowledged)
                .SetProperty(s => s.RespondedAtUtc, respondedAtUtc),
                cancellationToken);

        return affected > 0;
    }

    public async Task<bool> TryAcknowledgeChainAsync(
        Guid chainId, string userId, string userName, DateTime atUtc, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<EscalationChain>()
            .Where(c => c.Id == chainId && c.Status == EscalationChainStatus.Running)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Status, EscalationChainStatus.Acknowledged)
                .SetProperty(c => c.AcknowledgedByUserId, userId)
                .SetProperty(c => c.AcknowledgedByUserName, userName)
                .SetProperty(c => c.AcknowledgedAtUtc, atUtc),
                cancellationToken);

        return affected > 0;
    }

    public async Task<int> CancelRemainingStagesAsync(Guid chainId, Guid exceptStageId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EscalationStage>()
            .Where(s => s.EscalationChainId == chainId
                && s.Id != exceptStageId
                && (s.Status == EscalationStageStatus.Pending || s.Status == EscalationStageStatus.Notified))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, EscalationStageStatus.Cancelled),
                cancellationToken);
    }

    public async Task<bool> TryExhaustChainAsync(Guid chainId, string outcomeReason, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<EscalationChain>()
            .Where(c => c.Id == chainId && c.Status == EscalationChainStatus.Running)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Status, EscalationChainStatus.Exhausted)
                .SetProperty(c => c.OutcomeReason, outcomeReason),
                cancellationToken);

        return affected > 0;
    }

    public async Task<bool> TrySupersedeChainAsync(Guid chainId, string outcomeReason, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<EscalationChain>()
            .Where(c => c.Id == chainId && c.Status == EscalationChainStatus.Running)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Status, EscalationChainStatus.Superseded)
                .SetProperty(c => c.OutcomeReason, outcomeReason),
                cancellationToken);

        return affected > 0;
    }

    public async Task<bool> TryCancelChainAsync(
        Guid chainId, string userId, string userName, string reason, DateTime atUtc, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Set<EscalationChain>()
            .Where(c => c.Id == chainId && c.Status == EscalationChainStatus.Running)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Status, EscalationChainStatus.Cancelled)
                .SetProperty(c => c.CancelledByUserId, userId)
                .SetProperty(c => c.CancelledByUserName, userName)
                .SetProperty(c => c.CancelReason, reason)
                .SetProperty(c => c.CancelledAtUtc, atUtc),
                cancellationToken);

        return affected > 0;
    }
}
