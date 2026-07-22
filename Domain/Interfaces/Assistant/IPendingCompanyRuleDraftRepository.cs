// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence gateway for company-rule draft rows. GetAsync returns the outstanding draft for a
/// user/conversation (or null); UpsertAsync stores exactly one row per (user, conversation), replacing
/// any prior draft; DeleteAsync hard-removes the row for a user/conversation; PruneExpiredAsync
/// hard-removes every row whose TTL elapsed.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingCompanyRuleDraftRepository
{
    Task<PendingCompanyRuleDraftRow?> GetAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default);

    Task UpsertAsync(PendingCompanyRuleDraftRow row, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default);

    Task PruneExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
