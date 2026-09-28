// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class PeriodResource
{
    public Guid Id { get; set; }

    public Guid IndividualPeriodId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public decimal FullHours { get; set; }
}
