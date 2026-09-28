// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class ShiftDatePair
{
    public Guid ShiftId { get; set; }

    public DateTime Date { get; set; }
}
