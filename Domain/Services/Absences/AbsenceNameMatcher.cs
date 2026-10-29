// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Matches an absence type against a name a user typed: equal, trimmed and case-insensitive, to its name or its
/// abbreviation in any stored language, and describes absence types by name for error messages (English first,
/// otherwise the first available language, sorted).
/// </summary>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Absences;

public static class AbsenceNameMatcher
{
    private const string NameSeparator = ", ";

    /// <param name="wanted">Trimmed name the user typed</param>
    public static bool Matches(Absence absence, string wanted)
    {
        return absence.Name.GetAllValues().Values.Any(value => SameText(value, wanted))
            || absence.Abbreviation.GetAllValues().Values.Any(value => SameText(value, wanted));
    }

    public static string DescribeNames(IEnumerable<Absence> absences) =>
        string.Join(NameSeparator, absences
            .Select(absence => absence.Name.En ?? absence.Name.GetValueOrFirstAvailable(null))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

    private static bool SameText(string? stored, string wanted) =>
        !string.IsNullOrWhiteSpace(stored) && string.Equals(stored.Trim(), wanted, StringComparison.OrdinalIgnoreCase);
}
