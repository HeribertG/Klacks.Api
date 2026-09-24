// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence of inbound clarifications. SELF-COMMITTING: every write method saves immediately
/// (callers are background services and a skill, and TryAddOpenAsync must see the unique violation of
/// the one-open-per-client index at once). Callers must not have unsaved staged changes in the same
/// scope when they call a write method, because its SaveChanges would flush them too. AddAsync rejects
/// Status == Open with ArgumentOutOfRangeException — an Open row must go through TryAddOpenAsync so the
/// partial unique index is always the one deciding whether a client may get a new open clarification.
/// CountAskedSinceAsync counts a round (excluding Suggested) as within the rate-limit window when either
/// it was asked since the given instant or it ended (ResolvedAt) since then — a completed round still
/// blocks a new question until its own end falls outside the window, not just its start.
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

    /// <summary>
    /// Retention step: sets original_text to the empty string on every closed round (Answered, Expired,
    /// TakenOver, Unresolved, soft-deleted rows included) whose resolved_at lies before the cutoff and
    /// whose text is not already empty. One atomic ExecuteUpdate; Open and Suggested rows are never
    /// touched, and the row with status, question and deadlines stays. Idempotent; returns the number of
    /// rows cleared.
    /// </summary>
    /// <param name="cutoffUtc">Rows resolved strictly before this UTC instant are cleared</param>
    Task<int> ClearOriginalTextAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
