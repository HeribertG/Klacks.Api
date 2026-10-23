// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.Shifts;

public interface IShiftSortingService
{
    IQueryable<Shift> ApplySorting(IQueryable<Shift> query, string orderBy, string sortOrder);
}