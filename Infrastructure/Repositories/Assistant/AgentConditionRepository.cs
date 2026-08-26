// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed condition ledger. Reads are AsNoTracking because every write here is either a conditional
/// UPDATE that never materialises the row or an insert of a fresh entity, so nothing would benefit from
/// the change tracker while a tracked-but-stale row could mislead a caller. TryTransitionAsync mirrors
/// ScheduledTaskRepository.TryClaimAsync: ExecuteUpdateAsync with the expected status in the WHERE
/// clause, winner decided by the affected row count.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentConditionRepository : IAgentConditionRepository
{
    private const string UniqueViolationSqlState = "23505";

    private static readonly string[] ContextRelevantSeverities = [AgentTriggerSeverity.High, AgentTriggerSeverity.Medium];

    private readonly DataBaseContext _context;

    public AgentConditionRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AgentCondition?> FindOpenByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        var terminalStatuses = AgentConditionStateMachine.TerminalStatuses;

        return await _context.AgentConditions
            .Where(c => c.Fingerprint == fingerprint && !terminalStatuses.Contains(c.Status))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AgentCondition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AgentConditions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<AgentCondition?> FindByScenarioIdAsync(Guid scenarioId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentConditions
            .Where(c => c.ScenarioId == scenarioId)
            .OrderByDescending(c => c.DetectedAtUtc)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AgentCondition>> GetOpenByKindAsync(string triggerKind, CancellationToken cancellationToken = default)
    {
        var terminalStatuses = AgentConditionStateMachine.TerminalStatuses;

        return await _context.AgentConditions
            .Where(c => c.TriggerKind == triggerKind && !terminalStatuses.Contains(c.Status))
            .OrderBy(c => c.DetectedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentCondition?> InsertAsync(AgentCondition condition, AgentConditionEvent detectionEvent, CancellationToken cancellationToken = default)
    {
        detectionEvent.ConditionId = condition.Id;

        await _context.AgentConditions.AddAsync(condition, cancellationToken);
        await _context.AgentConditionEvents.AddAsync(detectionEvent, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return condition;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            Detach(condition, detectionEvent);
            return null;
        }
        catch
        {
            Detach(condition, detectionEvent);
            throw;
        }
    }

    public async Task<bool> TryTransitionAsync(
        Guid id,
        AgentConditionStatus fromStatus,
        AgentConditionStatus toStatus,
        AgentConditionTransitionFields? fields,
        AgentConditionEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        var resolvedAtUtc = fields?.ResolvedAtUtc;
        var handledAtUtc = fields?.HandledAtUtc;
        var escalatedAtUtc = fields?.EscalatedAtUtc;
        var scenarioId = fields?.ScenarioId;
        var handlingKind = fields?.HandlingKind;
        var rejectReason = fields?.RejectReason;
        var rejectedByUserId = fields?.RejectedByUserId;
        var lastAttemptAtUtc = fields?.LastAttemptAtUtc;
        var attemptIncrement = fields?.AttemptIncrement ?? 0;
        var approvedByUserId = fields?.ApprovedByUserId;

        auditEvent.ConditionId = id;

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var affected = await _context.AgentConditions
                .Where(c => c.Id == id && c.Status == fromStatus)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.Status, toStatus)
                        .SetProperty(c => c.ResolvedAtUtc, c => resolvedAtUtc ?? c.ResolvedAtUtc)
                        .SetProperty(c => c.HandledAtUtc, c => handledAtUtc ?? c.HandledAtUtc)
                        .SetProperty(c => c.EscalatedAtUtc, c => escalatedAtUtc ?? c.EscalatedAtUtc)
                        .SetProperty(c => c.ScenarioId, c => scenarioId ?? c.ScenarioId)
                        .SetProperty(c => c.HandlingKind, c => handlingKind ?? c.HandlingKind)
                        .SetProperty(c => c.RejectReason, c => rejectReason ?? c.RejectReason)
                        .SetProperty(c => c.RejectedByUserId, c => rejectedByUserId ?? c.RejectedByUserId)
                        .SetProperty(c => c.LastAttemptAtUtc, c => lastAttemptAtUtc ?? c.LastAttemptAtUtc)
                        .SetProperty(c => c.ApprovedByUserId, c => approvedByUserId ?? c.ApprovedByUserId)
                        .SetProperty(c => c.AttemptCount, c => c.AttemptCount + attemptIncrement),
                    cancellationToken);

            if (affected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await _context.AgentConditionEvents.AddAsync(auditEvent, cancellationToken);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                Detach(auditEvent);
                throw;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task<bool> TryReclaimStaleAsync(
        Guid id,
        DateTime staleBeforeUtc,
        DateTime claimedAtUtc,
        AgentConditionEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        auditEvent.ConditionId = id;

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var affected = await _context.AgentConditions
                .Where(c => c.Id == id
                    && c.Status == AgentConditionStatus.Prepared
                    && c.LastAttemptAtUtc != null
                    && c.LastAttemptAtUtc < staleBeforeUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.LastAttemptAtUtc, claimedAtUtc)
                        .SetProperty(c => c.AttemptCount, c => c.AttemptCount + 1),
                    cancellationToken);

            if (affected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await _context.AgentConditionEvents.AddAsync(auditEvent, cancellationToken);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                Detach(auditEvent);
                throw;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task<bool> TrySetCausedByAsync(
        Guid id,
        Guid causedByConditionId,
        CancellationToken cancellationToken = default)
    {
        var affected = await _context.AgentConditions
            .Where(c => c.Id == id && c.CausedByConditionId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.CausedByConditionId, causedByConditionId),
                cancellationToken);

        return affected > 0;
    }

    public async Task<List<AgentCondition>> GetActionableByKindAsync(
        string triggerKind,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.AgentConditions
            .Where(c => c.TriggerKind == triggerKind
                && (c.Status == AgentConditionStatus.Reported || c.Status == AgentConditionStatus.Prepared))
            .OrderBy(SeverityRank)
            .ThenBy(c => c.DetectedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActionClaimsAsync(
        string triggerKind,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        var claimPrefix = AgentConditionActionDefaults.ActionClaimDetailPrefix;

        return await (
            from conditionEvent in _context.AgentConditionEvents
            join condition in _context.AgentConditions
                on conditionEvent.ConditionId equals condition.Id
            where condition.TriggerKind == triggerKind
                && conditionEvent.AtUtc >= sinceUtc
                && conditionEvent.Detail != null
                && conditionEvent.Detail.StartsWith(claimPrefix)
            select conditionEvent.Id)
            .CountAsync(cancellationToken);
    }

    public async Task<List<AgentCondition>> GetExecutedSinceAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.AgentConditions
            .Where(c => c.Status == AgentConditionStatus.Executed
                && c.HandledAtUtc != null
                && c.HandledAtUtc >= sinceUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The filter admits a row whose LastSeenAtUtc is already current when a payload refresh is pending,
    /// because two detector ticks can share a timestamp - a test driving a fake clock always does, and so
    /// do two API instances scanning within the same clock resolution. Making the timestamp the sole gate
    /// would silently drop exactly those refreshes. Monotonicity is preserved in the setter instead,
    /// which never lowers the stored value.
    /// </summary>
    public async Task<bool> TouchLastSeenAsync(
        Guid id,
        DateTime seenAtUtc,
        string? payloadJson = null,
        CancellationToken cancellationToken = default)
    {
        var terminalStatuses = AgentConditionStateMachine.TerminalStatuses;

        var affected = await _context.AgentConditions
            .Where(c => c.Id == id
                && !terminalStatuses.Contains(c.Status)
                && (c.LastSeenAtUtc < seenAtUtc || payloadJson != null))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.LastSeenAtUtc, c => seenAtUtc > c.LastSeenAtUtc ? seenAtUtc : c.LastSeenAtUtc)
                    .SetProperty(c => c.PayloadJson, c => payloadJson ?? c.PayloadJson),
                cancellationToken);

        return affected > 0;
    }

    public async Task<bool> SetDelegationAsync(
        Guid id,
        ProactiveMaxAction maxAction,
        Guid delegatingUserId,
        CancellationToken cancellationToken = default)
    {
        var plannerRelevantStatuses = AgentConditionPlannerRelevantStatuses.Values;

        var affected = await _context.AgentConditions
            .Where(c => c.Id == id && plannerRelevantStatuses.Contains(c.Status))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.DelegatedMaxAction, maxAction)
                    .SetProperty(c => c.DelegatedByUserId, delegatingUserId),
                cancellationToken);

        return affected > 0;
    }

    public async Task<AgentConditionEvent> InsertEventAsync(AgentConditionEvent conditionEvent, CancellationToken cancellationToken = default)
    {
        await _context.AgentConditionEvents.AddAsync(conditionEvent, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            Detach(conditionEvent);
            throw;
        }

        return conditionEvent;
    }

    public async Task<IReadOnlyList<AgentCondition>> GetTopForContextAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        Guid? preferredGroupId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ScopedPlannerRelevantQuery(isUnrestricted, visibleRootIds)
            .Where(c => ContextRelevantSeverities.Contains(c.Severity));

        return await query
            .OrderBy(c => preferredGroupId.HasValue && c.GroupId == preferredGroupId ? 0 : 1)
            .ThenBy(c => c.Severity == AgentTriggerSeverity.High ? 0 : 1)
            .ThenBy(c => c.DetectedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AgentCondition>> GetOpenForScopeAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await ScopedPlannerRelevantQuery(isUnrestricted, visibleRootIds)
            .OrderBy(SeverityRank)
            .ThenBy(c => c.DetectedAtUtc)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountOpenForScopeAsync(
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        CancellationToken cancellationToken = default)
    {
        return await ScopedPlannerRelevantQuery(isUnrestricted, visibleRootIds).CountAsync(cancellationToken);
    }

    public async Task<AgentCondition?> GetOpenForScopeByIdAsync(
        Guid id,
        bool isUnrestricted,
        IReadOnlySet<Guid> visibleRootIds,
        CancellationToken cancellationToken = default)
    {
        return await ScopedPlannerRelevantQuery(isUnrestricted, visibleRootIds)
            .Where(c => c.Id == id)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static readonly Expression<Func<AgentCondition, int>> SeverityRank =
        c => c.Severity == AgentTriggerSeverity.High ? 0 : c.Severity == AgentTriggerSeverity.Medium ? 1 : 2;

    /// <summary>
    /// Base query shared by every planner-facing read of the ledger (Etappe 3f/3g): the
    /// AgentConditionPlannerRelevantStatuses.Values status filter, plus - when not unrestricted - the same
    /// GroupId-to-Group left join and root comparison GetTopForContextAsync originally introduced.
    ///
    /// A null GroupId is ungated for a genuinely installation-wide kind (target_hours_drift and the other
    /// client- or period-borne findings) and withheld for an AgentTriggerGroupScopedKinds.Values kind, where
    /// it can only mean the group of a group-owned entity was not determined - historical rows predating the
    /// live-push fix keep a null GroupId for as long as they stay open, because a re-detection refreshes only
    /// LastSeenAtUtc and PayloadJson, never GroupId. Handing those to every scoped planner would leak exactly the
    /// group-scoped detail the join below withholds, so they fall back to Admins, who take the isUnrestricted
    /// branch and skip this filter entirely - the same fallback the live push applies via
    /// IAgentTriggerEvent.RequiresGroupScope.
    /// </summary>
    private IQueryable<AgentCondition> ScopedPlannerRelevantQuery(bool isUnrestricted, IReadOnlySet<Guid> visibleRootIds)
    {
        IQueryable<AgentCondition> query = _context.AgentConditions
            .Where(c => AgentConditionPlannerRelevantStatuses.Values.Contains(c.Status));

        if (!isUnrestricted)
        {
            // Deliberately a separate Where ahead of the join rather than an extra term inside its
            // predicate: the join's "c.GroupId == null || ..." short circuits before the outer side's
            // "g.Root ?? g.Id" is touched, and folding the kind test into that disjunction makes the
            // GroupId-null rows reach the fallback with a null g - which real Postgres answers with SQL
            // null semantics but the EF InMemory provider throws on. Filtering first keeps the proven join
            // untouched and leaves no such row for it to see.
            query = query.Where(c => c.GroupId != null || !AgentTriggerGroupScopedKinds.Values.Contains(c.TriggerKind));

            query =
                from c in query
                join g in _context.Group on c.GroupId equals g.Id into groupJoin
                from g in groupJoin.DefaultIfEmpty()
                where c.GroupId == null || visibleRootIds.Contains(g.Root ?? g.Id)
                select c;
        }

        return query;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState == UniqueViolationSqlState;

    private void Detach(params object[] entities)
    {
        foreach (var entity in entities)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }
    }
}
