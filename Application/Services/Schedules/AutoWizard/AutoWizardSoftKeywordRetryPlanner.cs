// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.DTOs.Schedules.KeywordWishes;
using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules.AutoWizard;

/// <summary>
/// Rules of the AutoWizard's soft-keyword retry, pure so they stay testable without a job runner. The strict run (Hard)
/// stays the result; a second chain in Soft mode is only worth its runtime when the start asked for it, the strict
/// scenario leaves slots open and the employees have restricting schedule commands. Its scenario is only offered when
/// it fills strictly more slots than the strict one.
/// </summary>
public static class AutoWizardSoftKeywordRetryPlanner
{
    /// <summary>
    /// True when the start asks for the retry and the strict scenario leaves slots open. Whether a restricting command
    /// exists is checked by the caller afterwards (it needs the database).
    /// </summary>
    /// <param name="request">The start request.</param>
    /// <param name="strictSummary">Summary of the strict final scenario; null when it could not be built (no retry then,
    /// there is nothing to compare against).</param>
    public static bool IsWanted(StartAutoWizardRequest request, ScenarioSummaryDto? strictSummary)
        => request.SoftKeywordRetry
            && request.KeywordMode == KeywordMode.Hard
            && strictSummary is { OpenSlots: > 0 };

    /// <summary>True when the soft scenario fills strictly more slots than the strict one.</summary>
    /// <param name="strictSummary">Summary of the strict final scenario.</param>
    /// <param name="softSummary">Summary of the soft final scenario; null when it could not be built (never kept then).</param>
    public static bool IsBetter(ScenarioSummaryDto strictSummary, ScenarioSummaryDto? softSummary)
        => softSummary is not null && softSummary.FilledSlots > strictSummary.FilledSlots;

    /// <summary>
    /// True when the soft scenario breaks no restricting wish (FREE, OnlyX and NoX all 0). Such a plan is as valid as a
    /// strict one: Soft mode only relaxes the restricting schedule commands, everything else stays hard. Unfulfilled
    /// positive wishes do not count, a strict plan may leave them open as well.
    /// </summary>
    /// <param name="softSummary">Summary of the soft final scenario; null or without wish counts never qualifies.</param>
    public static bool BreaksNoRestriction(ScenarioSummaryDto? softSummary)
        => softSummary?.KeywordWishes is { } wishes && BrokenRestrictions(wishes) == 0;

    /// <summary>Broken restricting wishes of a plan: FREE, OnlyX and NoX together.</summary>
    /// <param name="wishes">Wish counts of the plan.</param>
    public static int BrokenRestrictions(KeywordWishesDto wishes)
        => wishes.BrokenFree + wishes.BrokenOnly + wishes.BrokenNo;

    /// <summary>
    /// Note written onto a promoted soft scenario: the second run found a better plan without breaking any wish.
    /// </summary>
    /// <param name="additionalFilledSlots">Slots it fills beyond the strict scenario.</param>
    /// <param name="strictScenarioName">Name of the strict scenario it replaces.</param>
    public static string BuildPromotedScenarioNote(int additionalFilledSlots, string strictScenarioName)
        => $"AutoWizard: a second run fills {additionalFilledSlots} more slot(s) than '{strictScenarioName}' without breaking " +
           "any schedule-command wish and replaces it as the result";

    /// <summary>
    /// The soft alternative with the employees' names on the wishes it misses beyond the strict scenario. A wish the strict
    /// scenario misses as well (an unfulfilled -FREE, a restriction broken by a fixed work) is not the soft plan's doing and
    /// is not listed; the full count stays in Summary.KeywordWishes.
    /// </summary>
    /// <param name="scenario">The soft final scenario.</param>
    /// <param name="strictSummary">Summary of the strict final scenario.</param>
    /// <param name="softSummary">Summary of the soft final scenario; its wish items carry the employees' names.</param>
    public static AutoWizardSoftAlternativeDto BuildAlternative(
        AutoWizardStageScenario scenario,
        ScenarioSummaryDto strictSummary,
        ScenarioSummaryDto softSummary)
        => new(
            scenario.ScenarioId,
            scenario.Token,
            scenario.Name,
            KeywordMode.Soft,
            softSummary.FilledSlots - strictSummary.FilledSlots,
            softSummary,
            SoftOnlyMisses(strictSummary, softSummary)
                .Select(item => new AutoWizardBrokenWishDto(
                    item.ClientId,
                    item.ClientName,
                    item.Date,
                    item.Keyword,
                    item.Outcome))
                .ToList());

    /// <summary>
    /// The wishes the soft scenario misses and the strict one does not, in the soft summary's order. A wish is the same
    /// when employee, day, keyword and outcome match; the display name takes no part.
    /// </summary>
    /// <param name="strictSummary">Summary of the strict final scenario.</param>
    /// <param name="softSummary">Summary of the soft final scenario.</param>
    public static IReadOnlyList<KeywordWishItemDto> SoftOnlyMisses(ScenarioSummaryDto strictSummary, ScenarioSummaryDto softSummary)
    {
        var strictMisses = (strictSummary.KeywordWishes?.Items ?? []).Select(WishKey).ToHashSet();
        return (softSummary.KeywordWishes?.Items ?? []).Where(item => !strictMisses.Contains(WishKey(item))).ToList();
    }

    /// <summary>
    /// Note written onto the soft scenario's description, so the assistant and the operator still recognise it after the
    /// job's terminal state has expired.
    /// </summary>
    /// <param name="additionalFilledSlots">Slots it fills beyond the strict scenario.</param>
    /// <param name="wishes">The wishes it misses.</param>
    /// <param name="strictScenarioName">Name of the strict scenario of the same run.</param>
    public static string BuildSoftScenarioNote(int additionalFilledSlots, KeywordWishesDto? wishes, string strictScenarioName)
        => $"AutoWizard soft-keyword alternative to '{strictScenarioName}': fills {additionalFilledSlots} more slot(s) " +
           $"by breaking schedule-command wishes (missed in total: FREE {wishes?.BrokenFree ?? 0}, only-kind {wishes?.BrokenOnly ?? 0}, " +
           $"excluded kind {wishes?.BrokenNo ?? 0}, unfulfilled {wishes?.Unfulfilled ?? 0})";

    /// <summary>
    /// Scenarios of the strict chain to delete once the run has finished: its intermediates, and its final scenario as
    /// well when a soft scenario was promoted in its place.
    /// </summary>
    /// <param name="strictProduced">Scenarios of the strict chain in the order the stages produced them.</param>
    /// <param name="promoted">True when the soft-keyword retry's scenario replaced the strict result.</param>
    public static IReadOnlyList<Guid> StrictScenariosToDelete(IReadOnlyList<AutoWizardStageScenario> strictProduced, bool promoted)
        => promoted
            ? strictProduced.Select(scenario => scenario.ScenarioId).ToList()
            : AutoWizardStageOutcomePlanner.ScenariosToDeleteOnSuccess(strictProduced);

    private static (Guid ClientId, DateOnly Date, string Keyword, string Outcome) WishKey(KeywordWishItemDto item)
        => (item.ClientId, item.Date, item.Keyword, item.Outcome);
}
