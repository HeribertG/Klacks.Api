// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftResetService
{
    /// <summary>
    /// Stages a new plannable shift with the content, groups and requirements of the sealed order and fresh copies of its
    /// default expenses.
    /// </summary>
    /// <param name="sealedOrder">The sealed order with its group items</param>
    /// <param name="newStartDate">First day of the new plannable shift</param>
    /// <param name="defaultExpenses">The stored default expense rows of the sealed order</param>
    Task<Shift> CreateNewOriginalShiftFromSealedOrderAsync(Shift sealedOrder, DateOnly newStartDate, IReadOnlyCollection<ShiftExpenses> defaultExpenses);
}
