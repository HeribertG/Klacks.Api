// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Final result of a harmonizer run, broadcast via SignalR and cached for status polls.
/// </summary>
/// <param name="JobId">Unique job identifier</param>
/// <param name="GlobalFitnessBefore">Weighted fitness of the source schedule</param>
/// <param name="GlobalFitnessAfter">Weighted fitness of the harmonised schedule</param>
/// <param name="GenerationsRun">Number of GA generations that actually executed</param>
/// <param name="RowResults">Per-agent before/after summary</param>
/// <param name="QualificationGaps">Assignments left in the final plan whose agent lacks a required mandatory qualification</param>
/// <param name="PlanningRuleWarnings">Approved hard planning constraints the run skipped because they are invalid
/// (key schedule.error-list.planning-rule-invalid); the plan honours every valid rule</param>
/// <param name="TimedOut">True when the loop stopped because the soft time budget elapsed; the result is the best arrangement found up to that point</param>
public sealed record HarmonizerJobResultDto(
    Guid JobId,
    double GlobalFitnessBefore,
    double GlobalFitnessAfter,
    int GenerationsRun,
    IReadOnlyList<HarmonizerRowResultDto> RowResults,
    IReadOnlyList<QualificationGapDetail> QualificationGaps,
    bool TimedOut = false,
    IReadOnlyList<ScheduleValidationNotificationDto>? PlanningRuleWarnings = null);
