// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public interface IGroupSearchRepository
{
    Task<GroupSearchResult> SearchAsync(
        string? searchTerm = null,
        int limit = Domain.Constants.PagingDefaults.DefaultSearchResultLimit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same search limited to the subtrees of the given root groups. The scope is part of the search, so the cap and the
    /// total count see only groups the caller may name.
    /// </summary>
    /// <param name="searchTerm">Text in the group name; null or blank = no name filter</param>
    /// <param name="limit">Maximum number of groups returned</param>
    /// <param name="visibleRootIds">Root groups in scope; null = every group, empty = no group</param>
    Task<GroupSearchResult> SearchWithinRootsAsync(
        string? searchTerm,
        int limit,
        IReadOnlyCollection<Guid>? visibleRootIds,
        CancellationToken cancellationToken = default);
}
