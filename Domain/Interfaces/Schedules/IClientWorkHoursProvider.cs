// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IClientWorkHoursProvider
{
    /// <summary>
    /// Sums a client's persisted Work hours (WorkTime) per day over a date range. Deliberately
    /// restricted to Work rows — Breaks and WorkChanges are excluded, consistent with the overtime
    /// surcharge engine's "Restricted to Work in this stage" scoping: absence hours (sickness,
    /// vacation) never produce overtime. Days without any Work are absent from the result.
    /// </summary>
    /// <param name="clientId">The client whose Work rows are summed</param>
    /// <param name="start">Inclusive first day of the range</param>
    /// <param name="end">Inclusive last day of the range</param>
    /// <param name="analyseToken">Scenario isolation: only rows with exactly this token (null = real mode) are counted</param>
    Task<IReadOnlyDictionary<DateOnly, decimal>> GetWorkHoursByDayAsync(Guid clientId, DateOnly start, DateOnly end, Guid? analyseToken);
}
