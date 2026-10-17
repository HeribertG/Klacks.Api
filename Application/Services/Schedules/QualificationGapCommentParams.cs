// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the CommentParams of a qualification-gap schedule-validation entry, shared by the pre-commit guardrail and
/// the live error list so both render the same text. An InsufficientLevel gap, and an Expired gap whose held level is
/// also too low, additionally carries the held level and the number of missing levels ("level 2 instead of 3").
/// </summary>
/// <param name="gap">The unmet requirement to describe</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using System.Globalization;

namespace Klacks.Api.Application.Services.Schedules;

public static class QualificationGapCommentParams
{
    public static Dictionary<string, string> Build(QualificationGap gap)
    {
        var parameters = new Dictionary<string, string>
        {
            [QualificationValidationParamKeys.QualificationId] = gap.QualificationId.ToString(),
            [QualificationValidationParamKeys.MinLevel] = gap.RequiredMinLevel.ToString(),
        };

        var carriesLevel = gap.Reason == QualificationGapReason.InsufficientLevel
            || (gap.Reason == QualificationGapReason.Expired && gap.LevelDelta > 0);
        if (carriesLevel && gap.HeldLevel.HasValue)
        {
            parameters[QualificationValidationParamKeys.HeldLevel] = gap.HeldLevel.Value.ToString();
            parameters[QualificationValidationParamKeys.LevelDelta] = gap.LevelDelta.ToString(CultureInfo.InvariantCulture);
        }

        return parameters;
    }
}
