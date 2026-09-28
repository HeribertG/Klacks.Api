// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class WorkResource : ScheduleEntryResource
{
    public ShiftResource? Shift { get; set; }

    public Guid ShiftId { get; set; }
}
