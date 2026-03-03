// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿namespace Klacks.Api.Domain.Services.Holidays;

public class HolidayDate
{
    public string CurrentName { get; set; } = string.Empty;

    public DateOnly CurrentDate { get; set; }

    public bool Officially { get; set; }

    public string FormatDate { get; set; } = string.Empty;
}
