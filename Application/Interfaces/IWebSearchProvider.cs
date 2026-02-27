// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IWebSearchProvider
{
    string ProviderName { get; }

    Task<DTOs.WebSearch.WebSearchResult> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken ct = default);
}
