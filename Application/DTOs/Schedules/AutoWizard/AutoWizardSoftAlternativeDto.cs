// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.DTOs.Schedules.AutoWizard;

/// <summary>
/// The second scenario of an AutoWizard run, planned in Soft keyword mode after the strict run left slots open. It is
/// only kept when it fills more slots than the strict scenario; it covers them by breaking schedule-command wishes.
/// </summary>
/// <param name="ScenarioId">Id of the soft scenario.</param>
/// <param name="ScenarioToken">Token of the soft scenario.</param>
/// <param name="ScenarioName">Display name of the soft scenario.</param>
/// <param name="KeywordMode">Always Soft; carried so a consumer never has to infer it.</param>
/// <param name="AdditionalFilledSlots">Slots the soft scenario fills beyond the strict one (always above 0).</param>
/// <param name="Summary">Live summary of the soft scenario (coverage, open slots, KeywordWishes).</param>
/// <param name="BrokenWishes">The missed wishes of the soft scenario with the employees' names, ordered by employee and day.</param>
public sealed record AutoWizardSoftAlternativeDto(
    Guid ScenarioId,
    Guid ScenarioToken,
    string ScenarioName,
    KeywordMode KeywordMode,
    int AdditionalFilledSlots,
    ScenarioSummaryDto Summary,
    IReadOnlyList<AutoWizardBrokenWishDto> BrokenWishes);
