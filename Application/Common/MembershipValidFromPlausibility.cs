// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure sanity check for a membership ValidFrom date (the plannability boundary). Flags a date that
/// lies before the employee's birthdate or more than the configured number of years in the past or
/// future, so an obvious typo can be caught before it silently makes the employee invisible for
/// scheduling.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Common;

public static class MembershipValidFromPlausibility
{
    /// <summary>
    /// Returns an English reason describing why the date is implausible, or <c>null</c> when it is
    /// within the accepted window.
    /// </summary>
    /// <param name="validFrom">The membership start date to check.</param>
    /// <param name="birthdate">The employee's birthdate, or <c>null</c> when unknown.</param>
    /// <param name="referenceDate">The "now" the past/future window is measured against.</param>
    public static string? Evaluate(DateTime validFrom, DateTime? birthdate, DateTime referenceDate)
    {
        if (birthdate.HasValue && validFrom.Date < birthdate.Value.Date)
        {
            return $"The start date {validFrom:yyyy-MM-dd} is before the employee's birthdate " +
                   $"{birthdate.Value:yyyy-MM-dd}.";
        }

        var earliest = referenceDate.AddYears(-MembershipPlausibilityDefaults.MaxYearsInPast).Date;
        if (validFrom.Date < earliest)
        {
            return $"The start date {validFrom:yyyy-MM-dd} is more than " +
                   $"{MembershipPlausibilityDefaults.MaxYearsInPast} years in the past.";
        }

        var latest = referenceDate.AddYears(MembershipPlausibilityDefaults.MaxYearsInFuture).Date;
        if (validFrom.Date > latest)
        {
            return $"The start date {validFrom:yyyy-MM-dd} is more than " +
                   $"{MembershipPlausibilityDefaults.MaxYearsInFuture} years in the future.";
        }

        return null;
    }
}
