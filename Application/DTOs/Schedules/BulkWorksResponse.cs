// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkWorksResponse : BulkScheduleEntryResponse
{
    public List<ShiftDatePair> AffectedShifts { get; set; } = [];
}
