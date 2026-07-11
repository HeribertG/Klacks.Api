// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the absence-type skills: resolves an absence type from a UUID or from a
/// name matched fuzzily (staged NameResolution) across all core-language names and
/// abbreviations, absorbing type-label words like "Absenztyp". Ambiguous or unknown names
/// return the real candidates instead of guessing.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Skills;

internal static class AbsenceTypeResolver
{
    private static readonly string[] LabelWords =
        ["absenz", "absenztyp", "abwesenheit", "absence", "absence type", "assenza"];

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

        var resolution = NameResolution.ResolveVariants(
            all,
            a => MultiLanguage.CoreLanguages.SelectMany(l =>
                new[] { a.Name.GetValue(l), a.Abbreviation.GetValue(l) }),
            query,
            LabelWords);

        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Candidates.Count > 1)
        {
            return (null,
                $"The absence type name '{query}' is ambiguous — it matches: " +
                string.Join(", ", resolution.Candidates.Select(a => $"{a.Name.De} ({a.Id})")) +
                ". Ask the user which one they mean — do not guess.");
        }

        var available = all.Count > 0
            ? "Available absence types: " + string.Join(", ", all.Select(a => a.Name.De)) + "."
            : "There are no absence types yet.";
        return (null, $"Absence type '{query}' not found. {available} Use list_absence_types for details.");
    }
}
