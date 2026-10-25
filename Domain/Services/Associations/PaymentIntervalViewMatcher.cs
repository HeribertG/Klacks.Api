// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule that decides whether a contract is seen by a schedule view with a given payment interval. Monthly
/// and MonthlyTargetHours contracts both follow the calendar month - the latter only takes its hours from the
/// company-wide table - so a view on either of them sees both (owner decision 2026-10-25, "Weg 1"); every other
/// interval matches itself only. Used by the contract data provider's contract queries and the schedule row flag,
/// so booking an absence and showing "has contract" never disagree.
/// </summary>
/// <param name="viewInterval">Payment interval of the schedule view or group, as the Ui sends it</param>
/// <param name="contractInterval">Payment interval of a contract</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Associations;

public static class PaymentIntervalViewMatcher
{
    private static readonly PaymentInterval[] CalendarMonthIntervals =
    [
        PaymentInterval.Monthly,
        PaymentInterval.MonthlyTargetHours
    ];

    /// <summary>The contract intervals a view interval accepts; usable inside an EF query via Contains.</summary>
    public static IReadOnlyList<PaymentInterval> AcceptedContractIntervals(int viewInterval)
    {
        if (viewInterval == (int)PaymentInterval.Monthly || viewInterval == (int)PaymentInterval.MonthlyTargetHours)
        {
            return CalendarMonthIntervals;
        }

        return Enum.IsDefined(typeof(PaymentInterval), viewInterval)
            ? [(PaymentInterval)viewInterval]
            : [];
    }

    public static bool Matches(PaymentInterval contractInterval, int viewInterval) =>
        AcceptedContractIntervals(viewInterval).Contains(contractInterval);
}
