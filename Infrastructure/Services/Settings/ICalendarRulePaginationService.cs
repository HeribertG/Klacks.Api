// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Infrastructure.Services.Settings;

public interface ICalendarRulePaginationService
{
    Task<TruncatedCalendarRule> ApplyPaginationAsync(IQueryable<CalendarRule> query, CalendarRulesFilter filter);
}