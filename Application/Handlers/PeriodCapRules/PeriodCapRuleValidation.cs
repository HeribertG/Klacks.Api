// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared field validation for the period cap rule Post/Put commands. Enforces the two mutually
/// exclusive modes: fixed-period (Period/Scope/CapHours) versus rolling-average (RollingWindowWeeks +
/// MaxAverageWeeklyHours), and the CustomWeeks window bounds.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.PeriodCapRules;

internal static class PeriodCapRuleValidation
{
    private const int MinCustomPeriodWeeks = 1;
    private const int MaxCustomPeriodWeeks = 104;
    private const int MinWarnAtPercent = 1;
    private const int MaxWarnAtPercent = 100;

    public static void Validate(PeriodCapRuleResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Period cap rule data is required.");
        }

        if (!Enum.IsDefined(resource.Period))
        {
            throw new InvalidRequestException("Period must be a defined period cap period.");
        }

        if (!Enum.IsDefined(resource.Scope))
        {
            throw new InvalidRequestException("Scope must be a defined period cap scope.");
        }

        var rollingRequested = resource.RollingWindowWeeks.HasValue || resource.MaxAverageWeeklyHours.HasValue;
        var fixedRequested = resource.CapHours > 0;

        if (rollingRequested && fixedRequested)
        {
            throw new InvalidRequestException("A period cap rule is either fixed-period (CapHours) or rolling-average (RollingWindowWeeks + MaxAverageWeeklyHours), not both.");
        }

        if (!rollingRequested && !fixedRequested)
        {
            throw new InvalidRequestException("A period cap rule must define either a fixed-period cap (CapHours) or a rolling-average cap (RollingWindowWeeks + MaxAverageWeeklyHours).");
        }

        if (rollingRequested)
        {
            if (resource.RollingWindowWeeks is null || resource.RollingWindowWeeks <= 0)
            {
                throw new InvalidRequestException("RollingWindowWeeks must be greater than zero in rolling-average mode.");
            }

            if (resource.MaxAverageWeeklyHours is null || resource.MaxAverageWeeklyHours <= 0)
            {
                throw new InvalidRequestException("MaxAverageWeeklyHours must be greater than zero in rolling-average mode.");
            }
        }

        if (resource.WarnAtPercent is < MinWarnAtPercent or > MaxWarnAtPercent)
        {
            throw new InvalidRequestException($"WarnAtPercent must be between {MinWarnAtPercent} and {MaxWarnAtPercent} when set.");
        }

        if (resource.Period == PeriodCapPeriod.CustomWeeks)
        {
            if (resource.CustomPeriodWeeks is null ||
                resource.CustomPeriodWeeks < MinCustomPeriodWeeks ||
                resource.CustomPeriodWeeks > MaxCustomPeriodWeeks)
            {
                throw new InvalidRequestException($"CustomPeriodWeeks is required and must be between {MinCustomPeriodWeeks} and {MaxCustomPeriodWeeks} when Period is CustomWeeks.");
            }
        }
        else if (resource.CustomPeriodWeeks.HasValue)
        {
            throw new InvalidRequestException("CustomPeriodWeeks may only be set when Period is CustomWeeks.");
        }
    }
}
