// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared helper for the calendar-selection-targeting skills: resolves a calendar selection by
/// id (takes precedence) or by a user-supplied name via the staged NameResolution matcher. When
/// several calendar selections remain plausible the resolver returns a disambiguation error
/// listing them instead of silently picking one. Under the default Strict policy a name that only a
/// fuzzy stage matched returns the ask-back text instead of the selection.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.CalendarSelections;

namespace Klacks.Api.Application.Skills;

internal static class CalendarSelectionResolver
{
    private static readonly string[] LabelWords = ["kalender", "kalenderauswahl", "calendar", "calendrier", "calendario"];

    /// <summary>
    /// Resolves a calendar selection from the already-extracted "id-or-name" skill parameters:
    /// calendarSelectionId (parsed as a UUID, takes precedence) or calendarSelectionName (staged
    /// fuzzy resolution via <see cref="Resolve"/>). Shared by every calendar-selection skill so
    /// the identification logic — and its error wording — stays in exactly one place.
    /// </summary>
    /// <param name="calendarSelectionId">Raw calendarSelectionId parameter value, or null/blank if omitted.</param>
    /// <param name="calendarSelectionName">Raw calendarSelectionName parameter value, or null/blank if omitted.</param>
    /// <param name="calendarSelections">Candidate calendar selections to resolve against (caller pre-filters deleted rows).</param>
    /// <param name="policy">Strict (default, every write) or Lenient (read-only skills only).</param>
    public static (CalendarSelection? Selection, string? Error) ResolveFromParameters(
        string? calendarSelectionId,
        string? calendarSelectionName,
        IReadOnlyList<CalendarSelection> calendarSelections,
        NameMatchPolicy policy = NameMatchPolicy.Strict)
    {
        if (!string.IsNullOrWhiteSpace(calendarSelectionId))
        {
            if (!Guid.TryParse(calendarSelectionId, out var id))
            {
                return (null, $"Invalid calendarSelectionId UUID: {calendarSelectionId}");
            }

            var found = calendarSelections.FirstOrDefault(c => c.Id == id);
            return found != null
                ? (found, null)
                : (null, $"Calendar selection with ID '{id}' not found.");
        }

        if (string.IsNullOrWhiteSpace(calendarSelectionName))
        {
            return (null, "Provide either calendarSelectionId or calendarSelectionName to identify the calendar selection.");
        }

        return Resolve(calendarSelections, calendarSelectionName, policy);
    }

    public static (CalendarSelection? Selection, string? Error) Resolve(
        IReadOnlyList<CalendarSelection> calendarSelections,
        string? calendarSelectionName,
        NameMatchPolicy policy = NameMatchPolicy.Strict)
    {
        var active = calendarSelections
            .Where(c => !c.IsDeleted && !string.IsNullOrWhiteSpace(c.Name))
            .ToList();
        var query = (calendarSelectionName ?? string.Empty).Trim();

        var resolution = NameResolution.Resolve(active, c => c.Name, query, LabelWords, policy);
        if (resolution.Match != null)
        {
            return (resolution.Match, null);
        }

        if (resolution.Suggestion != null)
        {
            return (null, NameResolutionMessages.SuggestedName(resolution.Suggestion.Name));
        }

        if (resolution.Candidates.Count > 1)
        {
            return (null,
                $"The calendar selection name '{query}' is ambiguous — it matches several calendar selections: " +
                string.Join(", ", resolution.Candidates.Select(c => c.Name)) + ". " +
                "Ask the user which exact calendar selection they mean — do not guess. " + NameResolutionAskBack.Marker);
        }

        var available = active.Count > 0
            ? "Available calendar selections: " + string.Join(", ", active.Select(c => c.Name)) + "."
            : "There are no calendar selections yet.";
        return (null,
            $"Calendar selection '{query}' not found. {available} " +
            "Do not call this skill again with the same name — pick the correct name from this list or ask " +
            "the user. Offer the user only these real calendar selection names — do not invent calendars.");
    }
}
