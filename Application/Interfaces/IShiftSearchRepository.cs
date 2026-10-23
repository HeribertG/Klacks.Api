// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public interface IShiftSearchRepository
{
    Task<ShiftSearchResult> SearchAsync(
        string? searchTerm = null,
        int limit = Domain.Constants.PagingDefaults.DefaultSearchResultLimit,
        CancellationToken cancellationToken = default);
}
