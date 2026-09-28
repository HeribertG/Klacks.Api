// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules;

public class ReassignWorkClientResponse
{
    public WorkResource? Work { get; set; }

    public List<WorkScheduleResource> SourceScheduleEntries { get; set; } = [];

    public PeriodHoursResource? SourcePeriodHours { get; set; }
}
