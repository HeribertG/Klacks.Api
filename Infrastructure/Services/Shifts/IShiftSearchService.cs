// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.Shifts;

public interface IShiftSearchService
{
    IQueryable<Shift> ApplySearchFilter(IQueryable<Shift> query, string searchString, bool includeClient);

    IQueryable<Shift> ApplyExactSearch(IQueryable<Shift> query, string[] keywords, bool includeClient);

    IQueryable<Shift> ApplyStandardSearch(IQueryable<Shift> query, string[] keywords, bool includeClient);

    IQueryable<Shift> ApplyFirstSymbolSearch(IQueryable<Shift> query, string symbol);
}
