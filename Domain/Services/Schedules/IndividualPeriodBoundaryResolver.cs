// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the pay-period boundaries of a custom (PaymentInterval.Individual) period definition
/// for a given date. Pure date arithmetic over the configured Period rows, no persistence access.
/// </summary>

using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class IndividualPeriodBoundaryResolver
{
    private const int DayBeforeSuccessor = -1;

    /// <summary>
    /// Returns start and end of the individual pay period covering <paramref name="date"/>.
    /// A Period matches when its FromDate is on or before the date and its UntilDate is unset
    /// (open ended) or on or after the date. When several Periods overlap the latest FromDate wins,
    /// so a correction added later takes precedence over the row it supersedes.
    /// An open ended Period ends the day before its successor starts; without a successor the
    /// definition is incomplete and cannot yield a bounded period.
    /// </summary>
    /// <param name="periods">All Period rows of the linked IndividualPeriod.</param>
    /// <param name="date">The date whose pay period is requested.</param>
    /// <param name="individualPeriodName">Only used to build actionable error messages.</param>
    public static (DateOnly StartDate, DateOnly EndDate) Resolve(
        IReadOnlyCollection<Period> periods,
        DateOnly date,
        string individualPeriodName)
    {
        ArgumentNullException.ThrowIfNull(periods);

        if (periods.Count == 0)
        {
            throw new InvalidRequestException(
                $"Individual period '{individualPeriodName}' has no periods defined. " +
                $"Define at least one period covering {date:yyyy-MM-dd}.");
        }

        var match = periods
            .Where(p => p.FromDate <= date && (p.UntilDate == null || p.UntilDate >= date))
            .OrderByDescending(p => p.FromDate)
            .ThenBy(p => p.Id)
            .FirstOrDefault();

        if (match == null)
        {
            throw new InvalidRequestException(
                $"Individual period '{individualPeriodName}' has no period covering {date:yyyy-MM-dd}. " +
                "Close the gap in the period definition.");
        }

        if (match.UntilDate.HasValue)
        {
            return (match.FromDate, match.UntilDate.Value);
        }

        var successorStart = periods
            .Where(p => p.FromDate > match.FromDate)
            .Select(p => (DateOnly?)p.FromDate)
            .Min();

        if (successorStart == null)
        {
            throw new InvalidRequestException(
                $"Individual period '{individualPeriodName}' ends with an open period starting " +
                $"{match.FromDate:yyyy-MM-dd} and has no following period. " +
                "Set an until date or add the next period to obtain a bounded pay period.");
        }

        return (match.FromDate, successorStart.Value.AddDays(DayBeforeSuccessor));
    }
}
