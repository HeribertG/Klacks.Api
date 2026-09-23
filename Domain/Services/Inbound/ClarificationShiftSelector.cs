// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Picks the shift a clarification question is about: the first planned shift (by date and start) that
/// has not ended yet at the given instant. Start and end are wall-clock times in the company time zone
/// and are converted to UTC via CompanyWallClockToUtcConverter; a shift whose end is not after its start
/// runs across midnight. A start inside the spring-forward gap advances to the next valid minute, a start
/// inside the fall-back overlap resolves to its earlier occurrence. Describe renders the shift as
/// "Name yyyy-MM-dd HH:mm-HH:mm" (name as stored) for the question prompt and the planner notification.
/// </summary>
/// <param name="shifts">Candidate shifts of the client in the search window</param>
/// <param name="nowUtc">The current instant (UTC)</param>
/// <param name="companyTimeZone">The company's configured time zone (ICompanyClock)</param>

using System.Globalization;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Domain.Services.Inbound;

public static class ClarificationShiftSelector
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";
    private const string NameSeparator = " ";
    private const string TimeRangeSeparator = "-";

    public static SelectedClarificationShift? SelectNext(
        IReadOnlyList<ClarificationShift> shifts, DateTime nowUtc, TimeZoneInfo companyTimeZone)
    {
        foreach (var shift in shifts.OrderBy(s => s.Date).ThenBy(s => s.StartTime))
        {
            var startUtc = ToUtc(shift.Date, shift.StartTime, companyTimeZone);
            var endDate = shift.EndTime <= shift.StartTime ? shift.Date.AddDays(1) : shift.Date;
            var endUtc = ToUtc(endDate, shift.EndTime, companyTimeZone);
            if (endUtc > nowUtc)
            {
                return new SelectedClarificationShift(startUtc, Describe(shift));
            }
        }

        return null;
    }

    public static DateTime ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo companyTimeZone)
    {
        return CompanyWallClockToUtcConverter.ConvertToUtc(
            date.ToDateTime(time, DateTimeKind.Unspecified), companyTimeZone);
    }

    public static string Describe(ClarificationShift shift)
    {
        var window =
            shift.Date.ToString(DateFormat, CultureInfo.InvariantCulture) + NameSeparator +
            shift.StartTime.ToString(TimeFormat, CultureInfo.InvariantCulture) + TimeRangeSeparator +
            shift.EndTime.ToString(TimeFormat, CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(shift.ShiftName) ? window : shift.ShiftName.Trim() + NameSeparator + window;
    }
}
