// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// W6.1 helper: aggregates the W1.6 toolset provenance snapshots. For every trajectory it looks up
/// the candidate whose name matches the chosen skill and counts its source, so the dashboard answers
/// "where did the winning skill come from" (AlwaysOn / Retrieved / Keyword / LearnedPhrase /
/// RecipeStep / Expansion / Hint).
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class SkillEffectivenessParser
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal static List<SkillEffectivenessSourceRow> DistributeChosenSources(
        IEnumerable<(string? ChosenSkill, string CandidatesJson)> trajectories)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (chosenSkill, candidatesJson) in trajectories)
        {
            var source = ResolveChosenSource(chosenSkill, candidatesJson);
            counts.TryGetValue(source, out var current);
            counts[source] = current + 1;
        }

        return counts
            .OrderByDescending(c => c.Value)
            .Select(c => new SkillEffectivenessSourceRow { Source = c.Key, Count = c.Value })
            .ToList();
    }

    internal static string ResolveChosenSource(string? chosenSkill, string candidatesJson)
    {
        const string unknown = "Unknown";

        if (string.IsNullOrWhiteSpace(chosenSkill))
        {
            return unknown;
        }

        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(candidatesJson) ? "[]" : candidatesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return unknown;
            }

            foreach (var candidate in document.RootElement.EnumerateArray())
            {
                var name = candidate.TryGetProperty("name", out var nameProperty) && nameProperty.ValueKind == JsonValueKind.String
                    ? nameProperty.GetString()
                    : null;

                if (!string.Equals(name, chosenSkill, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var source = candidate.TryGetProperty("source", out var sourceProperty) && sourceProperty.ValueKind == JsonValueKind.String
                    ? sourceProperty.GetString()
                    : null;

                return string.IsNullOrWhiteSpace(source) ? unknown : source;
            }

            return unknown;
        }
        catch (JsonException)
        {
            return unknown;
        }
    }
}
