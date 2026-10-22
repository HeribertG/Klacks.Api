// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves an applied company rule from a user-supplied name via the shared staged NameResolution
/// matcher. Returns a disambiguation error listing the candidates when several rules remain plausible,
/// and reports the real rule names when none match, so revert never targets the wrong rule. Under the
/// default Strict policy a name that only a fuzzy stage matched returns the ask-back text instead.
/// </summary>
/// <param name="policy">Strict (default, every write) or Lenient (read-only skills only).</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.Api.Application.Skills.CompanyRules;

internal static class CompanyRuleResolver
{
    public static (CompanyRuleResource? Rule, string? Error) Resolve(
        IReadOnlyList<CompanyRuleResource> rules, string? name, NameMatchPolicy policy = NameMatchPolicy.Strict)
    {
        var named = rules
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .ToList();
        var query = (name ?? string.Empty).Trim();

        var resolution = NameResolution.Resolve(named, r => r.Name, query, policy: policy);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Suggestion != null)
        {
            return (null, NameResolutionMessages.SuggestedName(resolution.Suggestion.Name));
        }

        if (resolution.Candidates.Count > 1)
        {
            return (null,
                $"The company-rule name '{query}' is ambiguous — it matches several rules: " +
                string.Join(", ", resolution.Candidates.Select(r => $"{r.Name} ({r.Id})")) + ". " +
                "Ask the user which exact rule they mean — do not guess. " + NameResolutionAskBack.Marker);
        }

        var available = named.Count > 0
            ? "Applied company rules: " + string.Join(", ", named.Select(r => r.Name)) + "."
            : "There are no applied company rules.";
        return (null,
            $"Company rule '{query}' not found. {available} " +
            "Pick the correct name from this list or ask the user — do not invent rules.");
    }
}
