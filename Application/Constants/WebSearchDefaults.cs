// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default number of results of a web search when the caller names none: the web_search skill and the search
/// provider contract both fall back to it.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class WebSearchDefaults
{
    public const int MaxResults = 5;
}
