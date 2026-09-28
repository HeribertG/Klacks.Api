// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces.CalendarSelections;

public interface ICalendarRulePaginationService
{
    Task<TruncatedCalendarRule> ApplyPaginationAsync(IQueryable<CalendarRule> query, CalendarRulesFilter filter);
}