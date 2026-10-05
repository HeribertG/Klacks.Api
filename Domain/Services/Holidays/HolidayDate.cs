// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Services.Holidays;

public class HolidayDate
{
    /// <summary>
    /// The holiday name in every language its calendar rule carries. Deliberately not resolved to one
    /// language here: the calculator does not know the reader, and findings built from it are broadcast
    /// to users of different languages, so each reader localizes at display time.
    /// </summary>
    public MultiLanguage Name { get; set; } = new();

    public DateOnly CurrentDate { get; set; }

    public bool Officially { get; set; }

    /// <summary>
    /// Copied from CalendarRule.IsPaid. Only an official AND paid holiday earns the holiday time surcharge.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// True when working on this holiday earns the holiday time surcharge: only an official AND paid holiday does.
    /// </summary>
    public bool EarnsTimeSurcharge => Officially && IsPaid;

    public string FormatDate { get; set; } = string.Empty;
}
