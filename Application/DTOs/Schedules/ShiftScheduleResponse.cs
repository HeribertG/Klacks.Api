// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class ShiftScheduleResponse
{
    public List<ShiftScheduleResource> Shifts { get; set; } = new();

    public int TotalCount { get; set; }
}
