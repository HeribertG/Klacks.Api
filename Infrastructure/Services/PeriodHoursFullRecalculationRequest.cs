// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services;

public class PeriodHoursFullRecalculationRequest
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
