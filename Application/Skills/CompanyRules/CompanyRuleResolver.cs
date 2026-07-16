// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves an applied company rule from a user-supplied name via the shared staged NameResolution
/// matcher. Returns a disambiguation error listing the candidates when several rules remain plausible,
/// and reports the real rule names when none match, so revert never targets the wrong rule.
/// </summary>

using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.Api.Application.Skills.CompanyRules;

internal static class CompanyRuleResolver
{
    public static (CompanyRuleResource? Rule, string? Error) Resolve(
        IReadOnlyList<CompanyRuleResource> rules, string? name)
    {
        var named = rules
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .ToList();
        var query = (name ?? string.Empty).Trim();

        var resolution = NameResolution.Resolve(named, r => r.Name, query);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Candidates.Count > 1)
        {
            return (null,
                $"The company-rule name '{query}' is ambiguous — it matches several rules: " +
                string.Join(", ", resolution.Candidates.Select(r => $"{r.Name} ({r.Id})")) + ". " +
                "Ask the user which exact rule they mean — do not guess.");
        }

        var available = named.Count > 0
            ? "Applied company rules: " + string.Join(", ", named.Select(r => r.Name)) + "."
            : "There are no applied company rules.";
        return (null,
            $"Company rule '{query}' not found. {available} " +
            "Pick the correct name from this list or ask the user — do not invent rules.");
    }
}
