// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Schedules;
namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkScheduleEntryResponse
{
    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    public List<Guid> CreatedIds { get; set; } = [];

    public List<Guid> DeletedIds { get; set; } = [];

    public Dictionary<Guid, PeriodHoursResource>? PeriodHours { get; set; }
}
