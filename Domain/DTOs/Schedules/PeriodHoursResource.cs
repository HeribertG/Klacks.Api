// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.Schedules;

public class PeriodHoursResource
{
    public decimal Hours { get; set; }
    public decimal Surcharges { get; set; }
    public decimal GuaranteedHours { get; set; }
}
