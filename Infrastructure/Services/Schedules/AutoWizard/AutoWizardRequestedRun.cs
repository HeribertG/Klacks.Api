// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.Summary;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// The AutoWizard chain in the requested keyword mode together with the reports on its final scenario.
/// </summary>
/// <param name="Chain">Final scenario, Wizard 1 compliance outcome and the stage-3 skip reason.</param>
/// <param name="QualificationGaps">Assignments of the final plan whose agent lacks a mandatory qualification.</param>
/// <param name="PlanningRuleWarnings">Invalid approved hard planning constraints the chain skipped.</param>
/// <param name="PlanningRuleRemaining">Hard planning-rule findings before and after; null when not counted.</param>
/// <param name="Summary">Live summary of the final scenario; null when it could not be built.</param>
internal sealed record AutoWizardRequestedRun(
    AutoWizardChainOutcome Chain,
    IReadOnlyList<QualificationGapDetail> QualificationGaps,
    IReadOnlyList<ScheduleValidationNotificationDto> PlanningRuleWarnings,
    PlanningRuleRemainingDto? PlanningRuleRemaining,
    ScenarioSummaryDto? Summary);
