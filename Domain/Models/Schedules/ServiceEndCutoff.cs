// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The company-local "now" split into the two values a database filter can compare a work end against.
/// </summary>
/// <param name="Today">Company-local calendar day</param>
/// <param name="Time">Company-local time of day</param>

namespace Klacks.Api.Domain.Models.Schedules;

public readonly record struct ServiceEndCutoff(DateOnly Today, TimeOnly Time)
{
    public static ServiceEndCutoff From(DateTime companyNow) =>
        new(DateOnly.FromDateTime(companyNow), TimeOnly.FromDateTime(companyNow));
}
