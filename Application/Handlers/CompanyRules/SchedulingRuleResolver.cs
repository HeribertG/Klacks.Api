// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a scheduling rule from a user-supplied name via the shared staged NameResolution matcher,
/// used to scope a counter-rule company rule to one industry. Returns a disambiguation error listing the
/// candidates when several rules remain plausible, and reports the real rule names when none match, so
/// the caller never silently picks the wrong scope.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Skills;

namespace Klacks.Api.Application.Handlers.CompanyRules;

internal static class SchedulingRuleResolver
{
    public static (SchedulingRuleResource? Rule, string? Error) Resolve(
        IReadOnlyList<SchedulingRuleResource> rules, string? ruleName)
    {
        var named = rules
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .ToList();
        var query = (ruleName ?? string.Empty).Trim();

        var resolution = NameResolution.Resolve(named, r => r.Name, query);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Candidates.Count > 1)
        {
            return (null,
                $"The scheduling-rule name '{query}' is ambiguous — it matches several rules: " +
                string.Join(", ", resolution.Candidates.Select(r => $"{r.Name} ({r.Id})")) + ". " +
                "Ask the user which exact scheduling rule they mean — do not guess.");
        }

        var available = named.Count > 0
            ? "Available scheduling rules: " + string.Join(", ", named.Select(r => r.Name)) + "."
            : "There are no scheduling rules yet.";
        return (null,
            $"Scheduling rule '{query}' not found. {available} " +
            "Pick the correct name from this list or ask the user — do not invent scheduling rules.");
    }
}
