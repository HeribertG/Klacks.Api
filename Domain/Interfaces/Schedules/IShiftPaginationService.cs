// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftPaginationService
{
    Task<TruncatedShift> ApplyPaginationAsync(IQueryable<Shift> filteredQuery, ShiftFilter filter);

    int CalculateFirstItem(ShiftFilter filter, int totalCount);
}