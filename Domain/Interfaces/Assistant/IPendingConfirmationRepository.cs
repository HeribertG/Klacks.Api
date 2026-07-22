// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence gateway for one-time confirmation-token rows. AddAsync stores a freshly issued token;
/// ConsumeAsync atomically reads and hard-deletes the row for a token in a single delete statement,
/// returning the row only if this call actually removed it (a concurrent second consume of the same
/// token loses the race and gets null), independent of any later user/skill/expiry validation so a
/// mismatched or expired token is still burned; GetActiveForUserAsync returns the user's outstanding
/// (not yet expired) tokens for PeekLatestForUser; PruneExpiredAsync hard-removes every row whose TTL
/// elapsed.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingConfirmationRepository
{
    Task AddAsync(PendingConfirmationRow row, CancellationToken cancellationToken = default);

    Task<PendingConfirmationRow?> ConsumeAsync(string token, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingConfirmationRow>> GetActiveForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
