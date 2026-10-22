// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared skill helper that resolves the shift a skill call names, by id or by name/abbreviation. Group visibility is
/// applied before anything is counted, limited or listed: the name search filters by the caller's group restriction in
/// the query itself (before its result limit), so a shift the caller may not see is left out of every match, can never
/// crowd a visible one out of the limit, and a hidden shift reads exactly like an unknown one, also in the ambiguity
/// list. The visibility guard filters the result once more as a second net. Several plannable pieces with the same name
/// (cuts of one shift) are narrowed to the piece valid on the requested date before the name counts as ambiguous;
/// genuine duplicates are answered with the candidate list, never resolved by picking one.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.ShiftSchedule;

namespace Klacks.Api.Application.Skills;

internal static class ShiftResolver
{
    private const int SearchResultLimit = 20;

    /// <summary>
    /// Resolves a shift by an id the model supplied, or by name or abbreviation.
    /// </summary>
    /// <param name="shiftGroupFilterService">Resolves the caller's group restriction the name search applies</param>
    /// <param name="shiftIdRaw">Shift id as the model supplied it; when present the name is ignored</param>
    /// <param name="shiftName">Shift name or abbreviation as the user said it</param>
    /// <param name="date">Day the shift is needed on; picks the cut valid on that day among same-named pieces</param>
    /// <param name="shiftIdParameterName">Name of the skill parameter carrying the id, named in the ambiguity answer</param>
    public static async Task<(Shift? Shift, string? Error)> ResolveByIdOrNameAsync(
        IShiftRepository shiftRepository,
        IShiftVisibilityGuard visibilityGuard,
        IShiftGroupFilterService shiftGroupFilterService,
        string? shiftIdRaw,
        string? shiftName,
        DateOnly date,
        string shiftIdParameterName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(shiftIdRaw))
        {
            if (!Guid.TryParse(shiftIdRaw, out var shiftId))
            {
                return (null, $"Invalid shift id '{shiftIdRaw}'. Expected a UUID, or give the shift name instead.");
            }

            if (!await visibilityGuard.ExistsAndVisibleAsync(shiftId, cancellationToken))
            {
                return (null, IdNotFoundMessage(shiftId));
            }

            var shift = await shiftRepository.Get(shiftId);
            return shift == null ? (null, IdNotFoundMessage(shiftId)) : (shift, null);
        }

        var term = shiftName?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return (null, "Please provide the shift name or its id.");
        }

        var visibleGroupIds = await shiftGroupFilterService.ResolveShiftListGroupIdsAsync(null);
        var matches = await shiftRepository.FindPlannableByNameAsync(term, visibleGroupIds, SearchResultLimit, cancellationToken);
        var visibleIds = matches.Count == 0
            ? new HashSet<Guid>()
            : await visibilityGuard.FilterVisibleAsync(matches.Select(s => s.Id).ToList(), cancellationToken);
        var candidates = matches.Where(s => visibleIds.Contains(s.Id)).ToList();

        if (candidates.Count == 0)
        {
            return (null, NameNotFoundMessage(term));
        }

        if (candidates.Count == 1)
        {
            return (candidates[0], null);
        }

        var validOnDate = candidates.Where(s => IsValidOn(s, date)).ToList();
        if (validOnDate.Count == 1)
        {
            return (validOnDate[0], null);
        }

        var pool = validOnDate.Count > 1 ? validOnDate : candidates;
        return (null,
            $"Multiple shifts match '{term}': {Describe(pool)}. Ask the user which one they mean, then call this skill " +
            $"again with that shift's id as '{shiftIdParameterName}'.");
    }

    /// <summary>The answer for an id that is unknown or hidden; both must read alike.</summary>
    /// <param name="shiftId">Shift id taken from the skill parameters</param>
    public static string IdNotFoundMessage(Guid shiftId) => $"Shift {shiftId} not found.";

    /// <summary>The answer for a name that matches no shift the caller may see.</summary>
    /// <param name="shiftName">Shift name as the caller supplied it</param>
    public static string NameNotFoundMessage(string shiftName) =>
        $"No shift found matching '{shiftName}'. Check the spelling with the user — " +
        "do not call this skill again with the same name.";

    private static bool IsValidOn(Shift shift, DateOnly date) =>
        shift.FromDate <= date && (shift.UntilDate == null || shift.UntilDate >= date);

    private static string Describe(IReadOnlyList<Shift> shifts) =>
        string.Join(", ", shifts.Select(s =>
            $"{s.Name} ({s.Abbreviation}, {s.FromDate.ToString(SkillDisplayFormats.IsoDate)} to " +
            $"{s.UntilDate?.ToString(SkillDisplayFormats.IsoDate) ?? SkillDisplayFormats.OpenEnd}, " +
            $"{s.StartShift.ToString(DateTimeFormats.HourMinute)}-" +
            $"{s.EndShift.ToString(DateTimeFormats.HourMinute)}, id {s.Id})"));
}
