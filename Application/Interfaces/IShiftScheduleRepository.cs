// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftScheduleRepository
{
    Task<(List<ShiftDayAssignment> Shifts, int TotalCount)> GetShiftScheduleAsync(
        ShiftScheduleFilter filter,
        CancellationToken cancellationToken);

    Task<List<ShiftDayAssignment>> GetShiftSchedulePartialAsync(
        ShiftSchedulePartialFilter filter,
        CancellationToken cancellationToken);
}
