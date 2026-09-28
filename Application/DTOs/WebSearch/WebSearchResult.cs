// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.WebSearch;

public class WebSearchResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public List<WebSearchEntry> Results { get; set; } = [];
}
