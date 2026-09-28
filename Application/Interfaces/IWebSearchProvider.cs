// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public interface IWebSearchProvider
{
    string ProviderName { get; }

    Task<DTOs.WebSearch.WebSearchResult> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken ct = default);
}
