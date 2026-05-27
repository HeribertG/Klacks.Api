// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Helper for normalizing LLM provider base URLs.
/// Provider base URLs must end with a trailing slash so that relative endpoints
/// (e.g. "models") resolve correctly via <see cref="System.Uri"/>.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class ProviderUrlHelper
{
    public static string EnsureTrailingSlash(string? baseUrl)
    {
        var trimmed = (baseUrl ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return trimmed.EndsWith('/') ? trimmed : trimmed + "/";
    }

    public static string NormalizeForComparison(string? baseUrl)
    {
        return (baseUrl ?? string.Empty).Trim().TrimEnd('/').ToLowerInvariant();
    }
}
