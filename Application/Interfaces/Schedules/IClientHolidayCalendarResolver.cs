// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single answer to "which days count as holidays for this client". Resolution order matches what the
/// surcharge calculation has always done: the contract's own calendar selection, otherwise the global
/// calendar selection, otherwise the global country/state pair - and a selection's OfficialOverride
/// wins over the calendar rule's own IsMandatory flag.
/// It exists so the surcharge path and the holiday-work detector cannot drift apart: a day paid as a
/// holiday must be the same day the detector calls a holiday.
/// </summary>

using Klacks.Api.Domain.Interfaces.CalendarSelections;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IClientHolidayCalendarResolver
{
    /// <summary>
    /// Returns the calculator for the given calendar selection and year, falling back to the global
    /// configuration. Null when the installation has no calendar configured at all.
    /// </summary>
    /// <param name="calendarSelectionId">The contract's calendar selection, null to use the global one</param>
    /// <param name="year">Holidays are computed per calendar year</param>
    Task<IHolidaysListCalculator?> GetCalculatorAsync(Guid? calendarSelectionId, int year);
}
