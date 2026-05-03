// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftFilterService
{
    IQueryable<Shift> ApplyAllFilters(IQueryable<Shift> query, ShiftFilter filter);
}