// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Schedules;
namespace Klacks.Api.Application.DTOs.Schedules;

public class WorkChangeClientResult
{
    public Guid ClientId { get; set; }

    public PeriodHoursResource? PeriodHours { get; set; }

    public List<WorkScheduleResource> ScheduleEntries { get; set; } = [];
}
