// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftResetService
{
    Task<Shift> CreateNewOriginalShiftFromSealedOrderAsync(Shift sealedOrder, DateOnly newStartDate);
    void CloseExistingSplitShifts(List<Shift> shifts, DateOnly closeDate);
}
