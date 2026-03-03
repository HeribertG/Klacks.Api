// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.WebSearch;

public class WebSearchResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public List<WebSearchEntry> Results { get; set; } = [];
}
