// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Name check for macros the assistant creates or renames: the name must not match another existing macro
/// (trimmed, case-insensitive) and must never be the name of a template shipped with Klacks, even when that
/// template was renamed or deleted. Returns an actionable refusal, or null when the name is free.
/// </summary>
/// <param name="macros">The existing macros</param>
/// <param name="name">The requested name</param>
/// <param name="ownMacroId">The macro being renamed, which may keep its own name; null for a new macro</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Skills;

public static class MacroNameCollision
{
    private const string NameTakenMessage =
        "A macro named '{0}' already exists. Choose a different name for this macro.";

    private const string TemplateNameMessage =
        "'{0}' is the name of a template macro shipped with Klacks and cannot be given to another macro. Choose a "
        + "different name for this macro.";

    public static string? FindRefusal(IEnumerable<MacroResource> macros, string name, Guid? ownMacroId)
    {
        var requested = name.Trim();
        if (macros.Any(m => m.Id != ownMacroId && IsSameName(m.Name, requested)))
        {
            return Format(NameTakenMessage, requested);
        }

        return SeededMacroNames.All.Any(template => IsSameName(template, requested))
            ? Format(TemplateNameMessage, requested)
            : null;
    }

    private static bool IsSameName(string existing, string requested) =>
        string.Equals(existing.Trim(), requested, StringComparison.OrdinalIgnoreCase);

    private static string Format(string format, string name) =>
        string.Format(CultureInfo.InvariantCulture, format, name);
}
