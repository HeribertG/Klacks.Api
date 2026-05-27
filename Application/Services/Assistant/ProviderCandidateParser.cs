// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses the raw JSON output of the provider-extraction LLM call into provider candidates.
/// Tolerates LLM noise (text/code fences around the JSON array) and sanitizes provider ids.
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class ProviderCandidateParser
{
    private const int MaxCandidates = 12;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public static List<ProviderCandidateResource> Parse(string? llmContent)
    {
        if (string.IsNullOrWhiteSpace(llmContent))
        {
            return [];
        }

        var cleaned = ExtractJsonArray(llmContent);
        if (cleaned.Length == 0)
        {
            return [];
        }

        List<ExtractedProvider>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ExtractedProvider>>(cleaned, JsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        if (items == null || items.Count == 0)
        {
            return [];
        }

        var result = new List<ProviderCandidateResource>();
        foreach (var item in items.Take(MaxCandidates))
        {
            var baseUrl = ProviderUrlHelper.EnsureTrailingSlash(item.BaseUrl);
            if (string.IsNullOrWhiteSpace(item.ProviderName) ||
                !Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                continue;
            }

            var providerId = SlugFromName(item.ProviderId, item.ProviderName);
            if (providerId.Length == 0)
            {
                continue;
            }

            result.Add(new ProviderCandidateResource
            {
                ProviderId = providerId,
                ProviderName = item.ProviderName.Trim(),
                BaseUrl = baseUrl,
                RequiresApiKey = item.RequiresApiKey,
                Source = ProviderCandidateSource.Web,
                Connectivity = ProviderConnectivityStatus.Unknown
            });
        }

        return result;
    }

    public static string SlugFromName(string? providerId, string providerName)
    {
        var source = string.IsNullOrWhiteSpace(providerId) ? providerName : providerId;
        var slug = new string((source ?? string.Empty).Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }

    private static string ExtractJsonArray(string content)
    {
        var start = content.IndexOf('[');
        var end = content.LastIndexOf(']');

        if (start < 0 || end < 0 || end <= start)
        {
            return string.Empty;
        }

        return content[start..(end + 1)];
    }

    private sealed record ExtractedProvider(
        string? ProviderId,
        string ProviderName,
        string BaseUrl,
        bool RequiresApiKey);
}
