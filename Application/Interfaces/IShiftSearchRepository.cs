// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public interface IShiftSearchRepository
{
    /// <summary>
    /// Searches the real plan only: rows with an analyse token (variant shifts, scenario clones) and scenario copies are
    /// never matched. The restriction is required so that every caller decides it; it applies before the count and the cap.
    /// </summary>
    /// <param name="visibleGroupIds">Flat restriction from IShiftGroupFilterService.ResolveShiftListGroupIdsAsync; null = unrestricted, empty = only group-less shifts</param>
    /// <param name="searchTerm">Words that must each match the abbreviation, the name or the client name</param>
    /// <param name="limit">Maximum number of listed items</param>
    Task<ShiftSearchResult> SearchAsync(
        IReadOnlyCollection<Guid>? visibleGroupIds,
        string? searchTerm = null,
        int limit = Domain.Constants.PagingDefaults.DefaultSearchResultLimit,
        CancellationToken cancellationToken = default);
}
