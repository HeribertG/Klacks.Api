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

    /// <summary>The soft alternative with the employees' names on its missed wishes.</summary>
    /// <param name="scenario">The soft final scenario.</param>
    /// <param name="strictSummary">Summary of the strict final scenario.</param>
    /// <param name="softSummary">Summary of the soft final scenario.</param>
    /// <param name="namesById">Display names of the employees; a missing one is shown as its id.</param>
    public static AutoWizardSoftAlternativeDto BuildAlternative(
        AutoWizardStageScenario scenario,
        ScenarioSummaryDto strictSummary,
        ScenarioSummaryDto softSummary,
        IReadOnlyDictionary<Guid, string> namesById)
        => new(
            scenario.ScenarioId,
            scenario.Token,
            scenario.Name,
            KeywordMode.Soft,
            softSummary.FilledSlots - strictSummary.FilledSlots,
            softSummary,
            MissedWishes(softSummary)
                .Select(item => new AutoWizardBrokenWishDto(
                    item.ClientId,
                    namesById.GetValueOrDefault(item.ClientId) ?? item.ClientId.ToString(),
                    item.Date,
                    item.Keyword,
                    item.Outcome))
                .ToList());

    /// <summary>The missed wishes of a summary; empty when it carries none.</summary>
    /// <param name="summary">A scenario summary.</param>
    public static IReadOnlyList<KeywordWishItemDto> MissedWishes(ScenarioSummaryDto summary)
        => summary.KeywordWishes?.Items ?? [];

    /// <summary>
    /// Note written onto the soft scenario's description, so the assistant and the operator still recognise it after the
    /// job's terminal state has expired.
    /// </summary>
    /// <param name="additionalFilledSlots">Slots it fills beyond the strict scenario.</param>
    /// <param name="wishes">The wishes it misses.</param>
    /// <param name="strictScenarioName">Name of the strict scenario of the same run.</param>
    public static string BuildSoftScenarioNote(int additionalFilledSlots, KeywordWishesDto? wishes, string strictScenarioName)
        => $"AutoWizard soft-keyword alternative to '{strictScenarioName}': fills {additionalFilledSlots} more slot(s) " +
           $"by breaking schedule-command wishes (FREE {wishes?.BrokenFree ?? 0}, only-kind {wishes?.BrokenOnly ?? 0}, " +
           $"excluded kind {wishes?.BrokenNo ?? 0}, unfulfilled {wishes?.Unfulfilled ?? 0})";
}
