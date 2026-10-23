// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Central .NET format strings for dates and times. Use these instead of inline format literals so wire, display and storage formats stay identical across the codebase.
/// </summary>
public static class DateTimeFormats
{
    public const string IsoDate = "yyyy-MM-dd";

    public const string HourMinute = "HH:mm";

    public const string HourMinuteSecond = "HH:mm:ss";

    public const string DottedDate = "dd.MM.yyyy";

    public const string IsoDateHourMinute = "yyyy-MM-dd HH:mm";

    public const string CompactDate = "yyyyMMdd";

    public static readonly string[] HourMinuteOrSecond = [HourMinute, HourMinuteSecond];
}
