// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Function names a model is known to use for a skill under a different name, mapped to the skill's real name.
/// The function executor normalizes a call with it before running it, and the toolset membership guard resolves
/// a call through it before checking it against the turn's toolset, so an alias of an offered skill is never
/// mistaken for a skill that was not offered.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SkillNameAliases
{
    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "get_current_user", "get_user_context" },
            { "getUserPermissions", "get_user_permissions" },
            { "create_client", "create_employee" },
            { "search_clients", "search_employees" },
            { "navigate_to_page", "navigate_to" }
        };

    /// <summary>
    /// The skill name a function name stands for: the real name of an alias, otherwise the name itself.
    /// </summary>
    /// <param name="functionName">The name the model used; null or empty resolves to an empty string</param>
    public static string Resolve(string? functionName) =>
        string.IsNullOrEmpty(functionName)
            ? string.Empty
            : Aliases.TryGetValue(functionName, out var skillName) ? skillName : functionName;

    /// <summary>
    /// True when the name is an alias and not the real name of a skill.
    /// </summary>
    /// <param name="functionName">The name the model used; null or empty is never an alias</param>
    /// <param name="skillName">The real skill name when the name is an alias, otherwise empty</param>
    public static bool TryResolve(string? functionName, out string skillName)
    {
        if (!string.IsNullOrEmpty(functionName) && Aliases.TryGetValue(functionName, out var resolved))
        {
            skillName = resolved;
            return true;
        }

        skillName = string.Empty;
        return false;
    }
}
