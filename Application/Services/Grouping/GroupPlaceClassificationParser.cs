// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses the LLM's group-place classification response into a <see cref="GroupPlaceClassification"/>.
/// Tolerant of common LLM output quirks: markdown code fences, leading/trailing prose, and unterminated
/// JSON (a missing closing brace, which some models emit). Any unrecoverable input yields
/// <see cref="GroupPlaceClassification.NotAPlace"/> so the caller safely treats it as "leave it".
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Services.Grouping;

public static class GroupPlaceClassificationParser
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static GroupPlaceClassification Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return GroupPlaceClassification.NotAPlace;
        }

        var cleaned = content.Trim();
        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline >= 0)
            {
                cleaned = cleaned[(firstNewline + 1)..];
            }
            cleaned = cleaned.TrimEnd('`', '\n', '\r', ' ');
        }

        var start = cleaned.IndexOf('{');
        if (start < 0)
        {
            return GroupPlaceClassification.NotAPlace;
        }

        var json = BalanceBraces(cleaned[start..].TrimEnd());

        try
        {
            var dto = JsonSerializer.Deserialize<ClassificationDto>(json, JsonOptions);
            if (dto == null)
            {
                return GroupPlaceClassification.NotAPlace;
            }

            var confidence = Math.Clamp(dto.Confidence, 0.0, 1.0);
            return new GroupPlaceClassification(dto.IsPlace, dto.CanonicalName, dto.Region, confidence);
        }
        catch (JsonException)
        {
            return GroupPlaceClassification.NotAPlace;
        }
    }

    private static string BalanceBraces(string json)
    {
        var lastClose = json.LastIndexOf('}');
        if (lastClose >= 0)
        {
            json = json[..(lastClose + 1)];
        }

        var open = json.Count(c => c == '{');
        var close = json.Count(c => c == '}');
        if (close < open)
        {
            json = json.TrimEnd(',', ' ', '\n', '\r', '\t') + new string('}', open - close);
        }

        return json;
    }

    private sealed class ClassificationDto
    {
        public bool IsPlace { get; set; }
        public string? CanonicalName { get; set; }
        public string? Region { get; set; }
        public double Confidence { get; set; }
    }
}
