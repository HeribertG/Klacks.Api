// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps application locales (e.g. "zh-TW", "nb") to Whisper language codes.
/// Whisper uses bare ISO 639-1 codes and knows Norwegian only as "no".
/// </summary>
/// <param name="locale">Application locale to map; null or empty yields an empty string</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

public static class WhisperLanguageMapper
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nb"] = "no",
        ["nn"] = "no",
    };

    public static string ToWhisperLanguage(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return string.Empty;
        }

        var baseCode = LanguageTag.BaseLanguage(locale)!.ToLowerInvariant();
        return Aliases.TryGetValue(baseCode, out var alias) ? alias : baseCode;
    }
}
