// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes a sealed order together with its plannable copy and cut pieces, but only while nothing is planned on them:
/// the usage check covers the whole family, so a work on a cut piece blocks the delete of the order. The shifts are
/// soft-deleted through the existing repository mechanics, which also soft-deletes their group links, default expenses and
/// required qualifications. The role of the caller is not checked here.
/// </summary>
/// <param name="shiftRepository">Reads the family usage and stages the delete</param>
/// <param name="unitOfWork">Commits the staged delete in one transaction</param>
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
    private readonly ILogger<SealedOrderFamilyDeletion> _logger;

    public SealedOrderFamilyDeletion(
        IShiftRepository shiftRepository,
        IUnitOfWork unitOfWork,
        ILogger<SealedOrderFamilyDeletion> logger)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
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

        await _shiftRepository.DeleteOrderFamilyAsync(usage.ShiftIds, cancellationToken);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Sealed order family deleted: Id={ShiftId}, Shifts={ShiftCount}", sealedOrder.Id, usage.ShiftIds.Count);
        return usage.ShiftIds.Count;
    }
}
