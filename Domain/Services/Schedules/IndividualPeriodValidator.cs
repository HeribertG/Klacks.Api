// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates the Period rows of an IndividualPeriod before persistence. Overlaps, gaps and
/// duplicate FromDate values are allowed and even expected (a later FromDate is a correction
/// that supersedes the row it overlaps, see IndividualPeriodBoundaryResolver), so this validator
/// only rejects structurally invalid rows.
/// </summary>

using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class IndividualPeriodValidator
{
    private const decimal MinimumFullHours = 0m;

    public static void Validate(IndividualPeriod individualPeriod)
    {
        ArgumentNullException.ThrowIfNull(individualPeriod);

        foreach (var period in individualPeriod.Periods)
        {
            if (period.FullHours < MinimumFullHours)
            {
                throw new InvalidRequestException(
                    $"Individual period '{individualPeriod.Name}' has a period starting {period.FromDate:yyyy-MM-dd} " +
                    $"with negative full hours ({period.FullHours}). Full hours must not be negative.");
            }

            if (period.UntilDate.HasValue && period.UntilDate.Value < period.FromDate)
            {
                throw new InvalidRequestException(
                    $"Individual period '{individualPeriod.Name}' has a period starting {period.FromDate:yyyy-MM-dd} " +
                    $"with an until date ({period.UntilDate.Value:yyyy-MM-dd}) before its from date. " +
                    "The until date must be on or after the from date.");
            }
        }
    }
}
