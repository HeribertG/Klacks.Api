// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared field validation for the restricted time window rule Post/Put commands. Season bounds are
/// validated as flat month/day tuples (1-12 / 1-31) to match how the domain stores the season. The daily
/// window follows the R1 duration convention: DailyEnd not greater than DailyStart wraps past midnight,
/// and equal bounds mean a full 24 hour ban window - exactly what CoreRestrictedTimeWindow and the
/// RestrictedTimeWindowEvaluator already compute, so every start/end pair is a valid window.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.RestrictedTimeWindowRules;

internal static class RestrictedTimeWindowRuleValidation
{
    private const int MinMonth = 1;
    private const int MaxMonth = 12;
    private const int MinDay = 1;
    private const int MaxDay = 31;

    public static void Validate(RestrictedTimeWindowRuleResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Restricted time window rule data is required.");
        }

        ValidateMonth(resource.SeasonFromMonth, nameof(resource.SeasonFromMonth));
        ValidateDay(resource.SeasonFromDay, nameof(resource.SeasonFromDay));
        ValidateMonth(resource.SeasonToMonth, nameof(resource.SeasonToMonth));
        ValidateDay(resource.SeasonToDay, nameof(resource.SeasonToDay));
    }

    private static void ValidateMonth(int value, string fieldName)
    {
        if (value < MinMonth || value > MaxMonth)
        {
            throw new InvalidRequestException($"{fieldName} must be between {MinMonth} and {MaxMonth}.");
        }
    }

    private static void ValidateDay(int value, string fieldName)
    {
        if (value < MinDay || value > MaxDay)
        {
            throw new InvalidRequestException($"{fieldName} must be between {MinDay} and {MaxDay}.");
        }
    }
}
