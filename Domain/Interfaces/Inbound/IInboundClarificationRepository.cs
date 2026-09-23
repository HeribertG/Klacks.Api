// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence of inbound clarifications. SELF-COMMITTING: every write method saves immediately
/// (callers are background services and a skill, and TryAddOpenAsync must see the unique violation of
/// the one-open-per-client index at once). Callers must not have unsaved staged changes in the same
/// scope when they call a write method, because its SaveChanges would flush them too. AddAsync rejects
/// Status == Open with ArgumentOutOfRangeException — an Open row must go through TryAddOpenAsync so the
/// partial unique index is always the one deciding whether a client may get a new open clarification.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundClarificationRepository
{
    Task<InboundClarification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InboundClarification?> GetOpenByClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<InboundClarification?> GetLatestExpiredByClientSinceAsync(
        Guid clientId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<InboundClarification?> GetLatestByEmailMessageIdsAsync(
        Guid clientId, IReadOnlyCollection<string> messageIds, CancellationToken cancellationToken = default);

    Task<InboundClarification?> GetByAnalysisIdAsync(Guid analysisId, CancellationToken cancellationToken = default);

    Task<int> CountAskedSinceAsync(Guid clientId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InboundClarification>> GetOpenDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<bool> TryAddOpenAsync(InboundClarification clarification, CancellationToken cancellationToken = default);

    Task AddAsync(InboundClarification clarification, CancellationToken cancellationToken = default);

    Task<bool> TryResolveAsync(
        Guid id,
        InboundClarificationStatus status,
        Guid? answerSourceId,
        Guid? resultAnalysisId,
        DateTime resolvedAtUtc,
        CancellationToken cancellationToken = default);
}
