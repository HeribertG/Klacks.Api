// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services;

public class PeriodHoursFullRecalculationRequest
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
