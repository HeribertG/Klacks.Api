// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates a company-wide monthly target hours row before persistence. Rows are sparse: only
/// months that actually override the contract target exist, so nothing enforces completeness of a
/// year. Only structurally invalid rows are rejected.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class MonthlyTargetHoursValidator
{
    public static void Validate(MonthlyTargetHours monthlyTargetHours)
    {
        ArgumentNullException.ThrowIfNull(monthlyTargetHours);

        if (monthlyTargetHours.Month < MonthlyTargetHoursConstants.MinimumMonth
            || monthlyTargetHours.Month > MonthlyTargetHoursConstants.MaximumMonth)
        {
            throw new InvalidRequestException(
                $"Monthly target hours have an invalid month ({monthlyTargetHours.Month}). " +
                $"The month must be between {MonthlyTargetHoursConstants.MinimumMonth} and " +
                $"{MonthlyTargetHoursConstants.MaximumMonth}.");
        }

        if (monthlyTargetHours.Year < MonthlyTargetHoursConstants.MinimumYear
            || monthlyTargetHours.Year > MonthlyTargetHoursConstants.MaximumYear)
        {
            throw new InvalidRequestException(
                $"Monthly target hours have an invalid year ({monthlyTargetHours.Year}). " +
                $"The year must be between {MonthlyTargetHoursConstants.MinimumYear} and " +
                $"{MonthlyTargetHoursConstants.MaximumYear}.");
        }

        if (monthlyTargetHours.Hours < MonthlyTargetHoursConstants.MinimumHours)
        {
            throw new InvalidRequestException(
                $"Monthly target hours for {monthlyTargetHours.Year}-{monthlyTargetHours.Month:00} " +
                $"are negative ({monthlyTargetHours.Hours}). Hours must not be negative.");
        }
    }
}
