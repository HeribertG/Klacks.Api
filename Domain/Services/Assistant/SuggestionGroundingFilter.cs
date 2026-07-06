// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filters LLM-generated suggestion-chip candidates down to the ones that actually match a real
/// entity name, using the same case-insensitive exact-or-substring rule the recipe skills use to
/// resolve a name to an entity (see AssignContractByNameSkill / GroupResolver). Pure and
/// dependency-free so it stays in the Domain layer.
/// </summary>

namespace Klacks.Api.Domain.Services.Assistant;

public static class SuggestionGroundingFilter
{
    public static List<string> Filter(IReadOnlyList<string> candidates, IReadOnlyList<string> realNames)
    {
        return candidates.Where(candidate => IsGrounded(candidate, realNames)).ToList();
    }

    private static bool IsGrounded(string candidate, IReadOnlyList<string> realNames)
    {
        var query = candidate.Trim();
        if (query.Length == 0)
        {
            return false;
        }

        return realNames.Any(name =>
            name.Trim().Equals(query, StringComparison.OrdinalIgnoreCase)
            || name.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
