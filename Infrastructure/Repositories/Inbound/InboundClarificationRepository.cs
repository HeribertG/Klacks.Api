// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for InboundClarification rows. Self-committing (see IInboundClarificationRepository).
/// TryAddOpenAsync relies on the partial unique index ix_inbound_clarifications_client_id_open and
/// answers false instead of throwing when another Open row of the same client won the race.
/// TryResolveAsync is a single conditional ExecuteUpdate scoped by Status == Open, mirroring the
/// escalation-chain transitions: when the expiry sweep and an incoming answer race for the same row,
/// exactly one of them moves it.
/// </summary>
/// <param name="context">The scoped database context</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Klacks.Api.Infrastructure.Repositories.Inbound;

public class InboundClarificationRepository : IInboundClarificationRepository
{
    private const string UniqueViolationSqlState = "23505";
    private const string TerminalStatusRequiredMessage = "A clarification can only be resolved to a terminal status.";

    private readonly DataBaseContext _context;

    public InboundClarificationRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<InboundClarification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InboundClarifications
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<InboundClarification?> GetOpenByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.InboundClarifications
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.Status == InboundClarificationStatus.Open, cancellationToken);
    }

    public async Task<InboundClarification?> GetLatestExpiredByClientSinceAsync(
        Guid clientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.InboundClarifications
            .AsNoTracking()
            .Where(c => c.ClientId == clientId
                        && c.Status == InboundClarificationStatus.Expired
                        && c.ResolvedAt != null
                        && c.ResolvedAt >= sinceUtc)
            .OrderByDescending(c => c.ResolvedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InboundClarification?> GetLatestByEmailMessageIdsAsync(
        Guid clientId, IReadOnlyCollection<string> messageIds, CancellationToken cancellationToken = default)
    {
        if (messageIds.Count == 0)
        {
            return null;
        }

        return await _context.InboundClarifications
            .AsNoTracking()
            .Where(c => c.ClientId == clientId && c.EmailMessageId != null && messageIds.Contains(c.EmailMessageId))
            .OrderByDescending(c => c.AskedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InboundClarification?> GetByAnalysisIdAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        return await _context.InboundClarifications
            .AsNoTracking()
            .Where(c => c.OriginalAnalysisId == analysisId || c.ResultAnalysisId == analysisId)
            .OrderByDescending(c => c.AskedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountAskedSinceAsync(Guid clientId, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _context.InboundClarifications
            .CountAsync(c => c.ClientId == clientId
                             && c.Status != InboundClarificationStatus.Suggested
                             && c.AskedAt >= sinceUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<InboundClarification>> GetOpenDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.InboundClarifications
            .AsNoTracking()
            .Where(c => c.Status == InboundClarificationStatus.Open && c.DeadlineAt <= nowUtc)
            .OrderBy(c => c.DeadlineAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryAddOpenAsync(InboundClarification clarification, CancellationToken cancellationToken = default)
    {
        clarification.Status = InboundClarificationStatus.Open;
        await _context.InboundClarifications.AddAsync(clarification, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _context.Entry(clarification).State = EntityState.Detached;
            return false;
        }
        catch
        {
            _context.Entry(clarification).State = EntityState.Detached;
            throw;
        }
    }

    public async Task AddAsync(InboundClarification clarification, CancellationToken cancellationToken = default)
    {
        await _context.InboundClarifications.AddAsync(clarification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryResolveAsync(
        Guid id,
        InboundClarificationStatus status,
        Guid? answerSourceId,
        Guid? resultAnalysisId,
        DateTime resolvedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (status is InboundClarificationStatus.Open or InboundClarificationStatus.Suggested)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, TerminalStatusRequiredMessage);
        }

        var affected = await _context.InboundClarifications
            .Where(c => c.Id == id && c.Status == InboundClarificationStatus.Open)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Status, status)
                .SetProperty(c => c.AnswerSourceId, answerSourceId)
                .SetProperty(c => c.ResultAnalysisId, resultAnalysisId)
                .SetProperty(c => c.ResolvedAt, resolvedAtUtc)
                .SetProperty(c => c.UpdateTime, resolvedAtUtc), cancellationToken);

        return affected == 1;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState == UniqueViolationSqlState;
}
