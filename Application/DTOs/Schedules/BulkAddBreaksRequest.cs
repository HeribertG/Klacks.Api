// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkAddBreaksRequest
{
    public List<BulkBreakItem> Breaks { get; set; } = [];

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public int? PaymentInterval { get; set; }
}
