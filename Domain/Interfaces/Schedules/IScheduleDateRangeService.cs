// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IScheduleDateRangeService
{
    (DateOnly startDate, DateOnly endDate) CalculateScheduleDateRange(ShiftScheduleFilter filter);
   
    IQueryable<Shift> ApplyScheduleDateFilter(IQueryable<Shift> query, DateOnly startDate, DateOnly endDate);
}