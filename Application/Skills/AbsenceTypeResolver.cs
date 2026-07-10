// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the absence-type skills: resolves an absence type from a UUID or from
/// a name matched case-insensitively across all core languages. Ambiguous or unknown names
/// return the real candidates instead of guessing.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Skills;

internal static class AbsenceTypeResolver
{
    public static async Task<(Absence? Absence, string? Error)> ResolveAsync(
        string? idRaw,
        string? nameRaw,
        IAbsenceRepository absenceRepository)
    {
        if (!string.IsNullOrWhiteSpace(idRaw))
        {
            if (!Guid.TryParse(idRaw, out var id))
            {
                return (null, $"Invalid absenceTypeId UUID: {idRaw}");
            }

            var byId = await absenceRepository.Get(id);
            return byId == null || byId.IsDeleted
                ? (null, $"Absence type with ID '{id}' not found.")
                : (byId, null);
        }

        if (string.IsNullOrWhiteSpace(nameRaw))
        {
            return (null, "Provide either absenceTypeId or typeName to identify the absence type.");
        }

        var all = (await absenceRepository.List()).Where(a => !a.IsDeleted).ToList();
        var query = nameRaw.Trim();

        var matches = all
            .Where(a => MultiLanguage.CoreLanguages.Any(l =>
                string.Equals(a.Name.GetValue(l), query, StringComparison.OrdinalIgnoreCase)
                || string.Equals(a.Abbreviation.GetValue(l), query, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (matches.Count == 1)
        {
            return (matches[0], null);
        }

        if (matches.Count > 1)
        {
            return (null,
                $"The absence type name '{query}' is ambiguous — it matches: " +
                string.Join(", ", matches.Select(a => $"{a.Name.De} ({a.Id})")) +
                ". Ask the user which one they mean — do not guess.");
        }

        var available = all.Count > 0
            ? "Available absence types: " + string.Join(", ", all.Select(a => a.Name.De)) + "."
            : "There are no absence types yet.";
        return (null, $"Absence type '{query}' not found. {available} Use list_absence_types for details.");
    }
}
