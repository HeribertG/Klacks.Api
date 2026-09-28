// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class MonthlyTargetHours : BaseEntity
{
    public int Year { get; set; }

    public int Month { get; set; }

    public decimal Hours { get; set; }
}
