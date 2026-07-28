// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules;

public class ReassignWorkClientResponse
{
    public WorkResource? Work { get; set; }

    public List<WorkScheduleResource> SourceScheduleEntries { get; set; } = [];
}
