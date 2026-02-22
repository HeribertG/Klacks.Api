// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;

namespace Klacks.Api.Application.Interfaces;

public interface ICalendarSelectionRepository : IBaseRepository<CalendarSelection>
{
    Task Update(CalendarSelection model);
    Task<CalendarSelection?> GetWithSelectedCalendars(Guid id);
    Task<List<Guid>> GetUsedByContractsAsync();
}
