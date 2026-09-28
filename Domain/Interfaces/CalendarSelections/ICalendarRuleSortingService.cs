// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.CalendarSelections;

public interface ICalendarRuleSortingService
{
    IQueryable<CalendarRule> ApplySorting(IQueryable<CalendarRule> query, string orderBy, string sortOrder, string language);
}