// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bounds of the replacement request book. MaxReportLeadDays: a report instant earlier than this many days
/// before the first covered slot is not a plausible sick call and is raised to that bound. MaxListSpanDays /
/// MaxListRows: the list endpoint needs an absent employee, a scenario or a date range of at most this span,
/// and never returns more than MaxListRows rows. MaxContactPastDays / MaxContactFutureDays: a contact attempt
/// may only be recorded for a slot within this window around the company's today.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ReplacementRequestLimits
{
    public const int MaxReportLeadDays = 92;
    public const int MaxListSpanDays = 93;
    public const int MaxListRows = 1000;
    public const int MaxContactPastDays = 31;
    public const int MaxContactFutureDays = 366;
}
