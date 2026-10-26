// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a turn must carry search_shifts: on the shift list page the user asks about the rows the list holds
/// ("wie viele Dienste in Bern", "welche Orte kommen vor"), and only search_shifts counts and reads shifts by location.
/// search_in_list merely sets the list filter in the browser and cannot read the result back, and vector retrieval does not
/// reliably rank search_shifts for such wording. The page alone is the signal, in any UI language. The skill only reads,
/// so the rule is liberal; a false positive costs one tool slot.
/// </summary>
/// <param name="currentRoute">Frontend route from the page context; may carry query strings, fragments or a trailing slash.</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ShiftListReadIntentResolver
{
    public const string SearchShiftsSkillName = "search_shifts";

    private static readonly string[] GuaranteedSkillsForPage = [SearchShiftsSkillName];

    public static IReadOnlyList<string> GuaranteedSkillNames(string? currentRoute) =>
        IsShiftListPage(currentRoute) ? GuaranteedSkillsForPage : [];

    /// <summary>True on the shift list page itself; edit and detail pages below it do not count.</summary>
    /// <param name="currentRoute">Frontend route from the page context</param>
    public static bool IsShiftListPage(string? currentRoute)
    {
        if (string.IsNullOrWhiteSpace(currentRoute))
        {
            return false;
        }

        var path = currentRoute.Split('?', '#')[0].TrimEnd('/');
        return string.Equals(path, ProactiveActionRoutes.ShiftList, StringComparison.OrdinalIgnoreCase);
    }
}
