// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.ShiftSchedule;

public interface IShiftScheduleSortingService
{
    IQueryable<ShiftDayAssignment> ApplySorting(IQueryable<ShiftDayAssignment> query, string? orderBy, string? sortOrder);
}
