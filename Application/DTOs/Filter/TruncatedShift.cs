// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedShift : BaseTruncatedResult
{
    public ICollection<Shift> Shifts { get; set; } = null!;
}
