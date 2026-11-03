// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

/// <summary>
/// Stages the plannable shift a reset ends with: the sealed order as it is, starting on the given day. ShiftCloner takes the
/// fields and groups, the requirements are copied from the sealed order, and the default expenses are fresh rows of the new
/// shift. What the planner changed on the plannable copy or on cut pieces does not survive.
/// </summary>
/// <param name="shiftRepository">Stages the new shift and its requirements</param>
/// <param name="logger">Logs the new shift</param>
public class ShiftResetService : IShiftResetService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<ShiftResetService> _logger;

    public ShiftResetService(
        IShiftRepository shiftRepository,
        ILogger<ShiftResetService> logger)
    {
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public async Task<Shift> CreateNewOriginalShiftFromSealedOrderAsync(
        Shift sealedOrder, DateOnly newStartDate, IReadOnlyCollection<ShiftExpenses> defaultExpenses)
    {
        _logger.LogInformation("Creating new OriginalShift from SealedOrder: {SealedOrderId}, NewStartDate: {NewStartDate}",
            sealedOrder.Id, newStartDate);

        var newOriginalShift = ShiftCloner.Clone(sealedOrder);
        newOriginalShift.Id = Guid.NewGuid();
        newOriginalShift.ShiftExpenses = ShiftDefaultExpenseCopier.CopyAll(defaultExpenses, newOriginalShift.Id);
        newOriginalShift.OriginalId = sealedOrder.Id;
        newOriginalShift.Status = ShiftStatus.OriginalShift;
        newOriginalShift.FromDate = newStartDate;
        newOriginalShift.UntilDate = sealedOrder.UntilDate;
        newOriginalShift.ParentId = null;
        newOriginalShift.RootId = null;
        newOriginalShift.Lft = null;
        newOriginalShift.Rgt = null;

        _logger.LogInformation("New OriginalShift created: ID={Id}, OriginalId={OriginalId}, FromDate={FromDate}, GroupItems count: {Count}",
            newOriginalShift.Id, newOriginalShift.OriginalId, newOriginalShift.FromDate, newOriginalShift.GroupItems.Count);

        await _shiftRepository.Add(newOriginalShift);
        await _shiftRepository.CopyRequiredQualificationsAsync(sealedOrder.Id, newOriginalShift.Id);

        return newOriginalShift;
    }
}
