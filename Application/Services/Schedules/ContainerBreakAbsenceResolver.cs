// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Chooses the absence type a container break is booked with. Only absence types that apply to containers count.
/// Without a name it is the seeded unpaid break, as long as it still exists and still applies to containers. With a
/// name it is the single type whose name or abbreviation equals it in any language (AbsenceNameMatcher); when none
/// is equal, the single type whose name contains it. No match or several matches end in an error naming the options.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Absences;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerBreakAbsenceResolver
{
    private const string NoneText = "none";

    /// <param name="absences">All absence types; the ones not applying to containers are ignored</param>
    /// <param name="absenceName">Name or abbreviation the user gave; null or blank selects the seeded unpaid break</param>
    public static (Absence? Absence, string? Error) Resolve(IEnumerable<Absence> absences, string? absenceName)
    {
        var containerAbsences = absences.Where(a => a.AppliesToContainer).ToList();

        if (string.IsNullOrWhiteSpace(absenceName))
        {
            var unpaidBreak = containerAbsences.FirstOrDefault(a => a.Id == SeededAbsenceIds.UnpaidBreak);
            return unpaidBreak is not null
                ? (unpaidBreak, null)
                : (null, "The default unpaid break absence type no longer exists or no longer applies to containers. " +
                    $"Give absenceName; absence types for containers: {Describe(containerAbsences)}.");
        }

        var wanted = absenceName.Trim();
        var matches = containerAbsences.Where(a => AbsenceNameMatcher.Matches(a, wanted)).ToList();
        if (matches.Count == 0)
        {
            matches = containerAbsences.Where(a => NameContains(a, wanted)).ToList();
        }

        return matches.Count switch
        {
            1 => (matches[0], null),
            0 => (null, $"No absence type for containers is named '{wanted}'. Absence types for containers: {Describe(containerAbsences)}."),
            _ => (null, $"Several absence types for containers match '{wanted}': {Describe(matches)}. Use the exact name.")
        };
    }

    private static bool NameContains(Absence absence, string wanted) =>
        absence.Name.GetAllValues().Values.Any(value =>
            !string.IsNullOrWhiteSpace(value) && value.Contains(wanted, StringComparison.OrdinalIgnoreCase));

    private static string Describe(IReadOnlyCollection<Absence> absences)
    {
        var names = AbsenceNameMatcher.DescribeNames(absences);
        return names.Length > 0 ? names : NoneText;
    }
}
