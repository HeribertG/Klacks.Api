// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared helper for the group-targeting skills: resolves a group from a user-supplied name via
/// the staged NameResolution matcher. An exact (accent-insensitive) name wins over partial
/// matches, a unique partial name still resolves, a query decorated with a label word (e.g.
/// "Gruppe Deutschschweiz Zürich" for the group "Deutschschweiz Zürich") resolves to the most
/// specific covered group, and lightly damaged input (missing accents, mojibake) is matched
/// fuzzily. When several groups remain plausible the resolver returns a disambiguation error
/// listing them instead of silently picking one; each candidate carries its parent and root group and
/// its id, because two groups may share the identical name under different parents.
/// </summary>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

internal static class GroupResolver
{
    internal static readonly string[] LabelWords = ["gruppe", "gruppen", "group", "groups", "groupe", "gruppo"];

    public static (Group? Group, string? Error) Resolve(
        IReadOnlyList<Group> groups, string? groupName)
    {
        var active = groups
            .Where(g => !g.IsDeleted && !string.IsNullOrWhiteSpace(g.Name))
            .ToList();
        var query = (groupName ?? string.Empty).Trim();

        var resolution = NameResolution.Resolve(active, g => g.Name, query, LabelWords);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Candidates.Count > 1)
        {
            var nameById = groups
                .Where(g => !string.IsNullOrWhiteSpace(g.Name))
                .GroupBy(g => g.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);
            return (null,
                $"The group name '{query}' is ambiguous — it matches several groups: " +
                string.Join("; ", resolution.Candidates.Select(g => DescribeCandidate(g, nameById))) + ". " +
                "Ask the user which exact group they mean (name the parent group to tell groups with the same name apart) — do not guess. " +
                "Where the skill accepts a groupId, pass the id of the chosen group.");
        }

        var available = active.Count > 0
            ? "Available groups: " + string.Join(", ", active.Select(g => g.Name)) + "."
            : "There are no groups yet.";
        return (null,
            $"Group '{query}' not found. {available} " +
            "Do not call this skill again with the same group name — pick the correct name from " +
            "this list or ask the user. Offer the user only these real group names — do not invent groups.");
    }

    private static string DescribeCandidate(Group group, IReadOnlyDictionary<Guid, string> nameById)
    {
        var parentName = group.Parent is { } parentId ? nameById.GetValueOrDefault(parentId) : null;
        var rootName = group.Root is { } rootId && rootId != group.Parent ? nameById.GetValueOrDefault(rootId) : null;

        var location = (parentName, rootName) switch
        {
            (not null, not null) => $" in '{parentName}' under '{rootName}'",
            (not null, null) => $" in '{parentName}'",
            (null, not null) => $" under '{rootName}'",
            _ => group.Parent is null ? " (top-level group)" : string.Empty
        };

        return $"'{group.Name}'{location}, id {group.Id}";
    }
}
