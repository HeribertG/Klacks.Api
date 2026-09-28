// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿namespace Klacks.Api.Domain.Services.Holidays;

public class HolidayDate
{
    public string CurrentName { get; set; } = string.Empty;

    public DateOnly CurrentDate { get; set; }

    public bool Officially { get; set; }

    public string FormatDate { get; set; } = string.Empty;
}
