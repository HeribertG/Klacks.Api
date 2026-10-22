// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Company-day key of the daily grouping feasibility snapshot (e.g. 2026-09-28), so the detector
/// recomputes at most once per company calendar day.
/// </summary>
/// <param name="day">Company calendar day (from ICompanyClock) that is keyed.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Grouping;

public static class GroupingFeasibilityDay
{
    public static string KeyFor(DateOnly day) => day.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
}
