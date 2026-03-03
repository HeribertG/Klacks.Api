// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftStatusFilterService
{
    IQueryable<Shift> ApplyStatusFilter(IQueryable<Shift> query, ShiftFilterType filterType, bool isSealedOrder = false, bool isTimeRange = true, bool isSporadic = true);
}