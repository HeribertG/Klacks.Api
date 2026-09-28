// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkWorksResponse : BulkScheduleEntryResponse
{
    public List<ShiftDatePair> AffectedShifts { get; set; } = [];
}
