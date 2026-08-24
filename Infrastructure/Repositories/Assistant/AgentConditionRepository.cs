// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed condition ledger. Reads are AsNoTracking because every write here is either a conditional
/// UPDATE that never materialises the row or an insert of a fresh entity, so nothing would benefit from
/// the change tracker while a tracked-but-stale row could mislead a caller. TryTransitionAsync mirrors
/// ScheduledTaskRepository.TryClaimAsync: ExecuteUpdateAsync with the expected status in the WHERE
/// clause, winner decided by the affected row count.
/// </summary>

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
                        .SetProperty(c => c.RejectedByUserId, c => rejectedByUserId ?? c.RejectedByUserId),
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

    public async Task<bool> TouchLastSeenAsync(Guid id, DateTime seenAtUtc, CancellationToken cancellationToken = default)
    {
        var terminalStatuses = AgentConditionStateMachine.TerminalStatuses;

        var affected = await _context.AgentConditions
            .Where(c => c.Id == id && c.LastSeenAtUtc < seenAtUtc && !terminalStatuses.Contains(c.Status))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.LastSeenAtUtc, seenAtUtc),
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
