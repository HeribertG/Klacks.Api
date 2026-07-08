// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the group-targeting skills: resolves a group from a user-supplied name via
/// the staged NameResolution matcher. An exact (accent-insensitive) name wins over partial
/// matches, a unique partial name still resolves, a query decorated with a label word (e.g.
/// "Gruppe Deutschschweiz Zürich" for the group "Deutschschweiz Zürich") resolves to the most
/// specific covered group, and lightly damaged input (missing accents, mojibake) is matched
/// fuzzily. When several groups remain plausible the resolver returns a disambiguation error
/// listing them instead of silently picking one.
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

        var resolution = NameResolution.Resolve(active, g => g.Name, query);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Candidates.Count > 1)
        {
            return (null,
                $"The group name '{query}' is ambiguous — it matches several groups: " +
                string.Join(", ", resolution.Candidates.Select(g => g.Name)) + ". " +
                "Ask the user which exact group they mean — do not guess.");
        }

        var available = active.Count > 0
            ? "Available groups: " + string.Join(", ", active.Select(g => g.Name)) + "."
            : "There are no groups yet.";
        return (null,
            $"Group '{query}' not found. {available} " +
            "Do not call this skill again with the same group name — pick the correct name from " +
            "this list or ask the user. Offer the user only these real group names — do not invent groups.");
    }
}
