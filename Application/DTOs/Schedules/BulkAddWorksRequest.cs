// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkAddWorksRequest
{
    public List<BulkWorkItem> Works { get; set; } = [];

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }
}
