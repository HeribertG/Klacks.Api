// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Severity of a day-directive conflict (<see cref="ScheduleValidationKeys.DayDirective"/>) for a plan produced in a given
/// keyword mode. In <see cref="KeywordMode.Soft"/> the wizard broke the wish on purpose (to cover a slot), so the conflict is
/// reported as a Warning and the placement is written; in every other mode it stays the Error the evaluator produced.
/// </summary>
public static class DayDirectiveWishSeverity
{
    /// <summary>The check result with every day-directive Error turned into a Warning when the mode is Soft.</summary>
    /// <param name="result">Pre-commit check result as the conflict checker returned it.</param>
    /// <param name="keywordMode">Keyword mode the planned rows were produced in.</param>
    public static PreCommitCheckResult Apply(PreCommitCheckResult result, KeywordMode keywordMode)
    {
        if (keywordMode != KeywordMode.Soft || !result.NewConflicts.Any(IsDirectiveError))
        {
            return result;
        }

        return new PreCommitCheckResult(result.NewConflicts.Select(ToWarning).ToList());
    }

    private static bool IsDirectiveError(ScheduleValidationNotificationDto entry)
        => entry.Type == ScheduleValidationType.Error && entry.Comment == ScheduleValidationKeys.DayDirective;

    private static ScheduleValidationNotificationDto ToWarning(ScheduleValidationNotificationDto entry)
        => IsDirectiveError(entry) ? entry with { Type = ScheduleValidationType.Warning } : entry;
}
