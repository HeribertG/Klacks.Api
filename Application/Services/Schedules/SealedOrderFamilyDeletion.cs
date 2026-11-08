// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes a sealed order together with its plannable copy and cut pieces, or a container, whose family is its own row,
/// but only while nothing is planned on them:
/// the usage check covers the whole family, so a work on a cut piece blocks the delete of the order. The shifts are
/// soft-deleted through the existing repository mechanics, which also soft-deletes their group links, default expenses and
/// required qualifications. The right of the caller is not checked here, but its group visibility is, over the whole
/// family: since a supervisor may delete order families (owner decision 2026-11-07), a group-restricted caller who sees
/// the addressed order must not take a plannable copy or cut piece of another group with it, so a family with a hidden
/// row is refused like one still in use. An unrestricted caller (Admin) is not affected.
/// </summary>
/// <param name="shiftRepository">Reads the family usage and stages the delete</param>
/// <param name="unitOfWork">Commits the staged delete in one transaction</param>
/// <param name="shiftVisibilityGuard">Refuses a family with a row the caller may not touch</param>
/// <param name="logger">Logs the refusal and the delete</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class SealedOrderFamilyDeletion : ISealedOrderFamilyDeletion
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly ILogger<SealedOrderFamilyDeletion> _logger;

    public SealedOrderFamilyDeletion(
        IShiftRepository shiftRepository,
        IUnitOfWork unitOfWork,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<SealedOrderFamilyDeletion> logger)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _logger = logger;
    }

    public async Task<int> DeleteAsync(Shift sealedOrder, CancellationToken cancellationToken = default)
    {
        var usage = await _shiftRepository.GetOrderFamilyUsageAsync(sealedOrder.Id, cancellationToken);
        if (usage.IsInUse)
        {
            _logger.LogWarning(
                "Sealed order family delete refused: Id={ShiftId}, Works={HasWorks}, ScenarioClones={HasScenarioClones}, Containers={HasContainers}",
                sealedOrder.Id, usage.HasWorks, usage.HasScenarioClones, usage.HasContainerReferences);
            throw ShiftLockedException.ForOrderFamilyInUse(sealedOrder, usage);
        }

        var hiddenShiftId = await _shiftVisibilityGuard.FindFirstHiddenAsync(usage.ShiftIds, cancellationToken);
        if (hiddenShiftId != null)
        {
            _logger.LogWarning(
                "Sealed order family delete refused: Id={ShiftId}, a family row outside the caller's groups ({HiddenShiftId})",
                sealedOrder.Id, hiddenShiftId);
            throw ShiftLockedException.ForDelete(sealedOrder);
        }

        await _shiftRepository.DeleteOrderFamilyAsync(usage.ShiftIds, cancellationToken);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Sealed order family deleted: Id={ShiftId}, Shifts={ShiftCount}", sealedOrder.Id, usage.ShiftIds.Count);
        return usage.ShiftIds.Count;
    }
}
