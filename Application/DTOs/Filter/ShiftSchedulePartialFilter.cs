// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Filter;

public class ShiftSchedulePartialFilter
{
    public List<ShiftDatePairFilter> ShiftDatePairs { get; set; } = [];

    public Guid? AnalyseToken { get; set; }
}

public class ShiftDatePairFilter
{
    public Guid ShiftId { get; set; }

    public DateTime Date { get; set; }
}
