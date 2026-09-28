// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class MonthlyTargetHoursResource
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal Hours { get; set; }
}
