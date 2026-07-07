// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the group-targeting skills: resolves a group from a user-supplied name.
/// An exact (case-insensitive, trimmed) name wins over partial matches, so a name like "Bern"
/// resolves to the group "Bern" even when "Bern-wöchentlich" also exists. A partial name still
/// resolves when it is unique; if it matches several groups the resolver returns an error that
/// asks the user to disambiguate instead of silently picking one.
/// </summary>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

internal static class GroupResolver
{
    public static (Group? Group, string? Error) Resolve(
        IReadOnlyList<Group> groups, string? groupName)
    {
        var active = groups
            .Where(g => !g.IsDeleted && !string.IsNullOrWhiteSpace(g.Name))
            .ToList();
        var query = (groupName ?? string.Empty).Trim();

        var exact = active
            .Where(g => g.Name.Trim().Equals(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exact.Count == 1)
        {
            return (exact[0], null);
        }

        var candidates = exact.Count > 1
            ? exact
            : active
                .Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (candidates.Count == 1)
        {
            return (candidates[0], null);
        }

        if (candidates.Count > 1)
        {
            return (null,
                $"The group name '{query}' is ambiguous — it matches several groups: " +
                string.Join(", ", candidates.Select(g => g.Name)) + ". " +
                "Ask the user which exact group they mean — do not guess.");
        }

        // No forward match (the actual name never contains the query): the model may have extracted the
        // user's phrase verbatim, including a leading label word like "Gruppe "/"Group " that is not part
        // of the real name (e.g. query "Gruppe Deutschschweiz Zürich" for the group "Deutschschweiz
        // Zürich"). Among names the QUERY contains, the LONGEST one wins rather than being treated as
        // ambiguous — a short code can accidentally be a substring of the label word itself (e.g. "GR"
        // inside "GRuppe"), but the longest contained name is the most complete, specific match.
        var reverseMatch = active
            .Where(g => query.Contains(g.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(g => g.Name.Length)
            .FirstOrDefault();
        if (reverseMatch != null)
        {
            return (reverseMatch, null);
        }

        var available = active.Count > 0
            ? "Available groups: " + string.Join(", ", active.Select(g => g.Name)) + "."
            : "There are no groups yet.";
        return (null,
            $"Group '{query}' not found. {available} " +
            "Offer the user only these real group names — do not invent groups.");
    }
}
