// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistence gateway for the per-conversation recent-entity ring. Add appends a row and hard-deletes
/// any rows beyond the retention bound for that (user, conversation); GetRecent returns the retained rows
/// newest-first for a (user, conversation).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IRecentEntityRepository
{
    Task AddAsync(RecentEntityRow row, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentEntityRow>> GetRecentAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default);
}
