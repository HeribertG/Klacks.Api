// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The owner change of a work: the person, the container children and the marker first, because the macros read the
/// contract of the new owner, then the recalculation in container order.
/// </summary>
/// <param name="cascadeService">Moves the container children to the new owner</param>
/// <param name="childrenManager">Recalculates the work and its children</param>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class WorkHandOverService : IWorkHandOverService
{
    private readonly IContainerWorkCascadeService _cascadeService;
    private readonly IContainerWorkChildrenManager _childrenManager;

    public WorkHandOverService(IContainerWorkCascadeService cascadeService, IContainerWorkChildrenManager childrenManager)
    {
        _cascadeService = cascadeService;
        _childrenManager = childrenManager;
    }

    public async Task<bool> TransferAsync(Work work, Guid targetClientId, Guid? movedFromClientId, CancellationToken cancellationToken)
    {
        var previousClientId = work.ClientId;
        work.ClientId = targetClientId;
        if (movedFromClientId is { } movedFrom)
        {
            work.MovedFromClientId = movedFrom;
        }

        await _cascadeService.MoveChildrenAsync(work.Id, work.CurrentDate, previousClientId, targetClientId);
        return await _childrenManager.RecalculateForNewOwnerAsync(work, cancellationToken);
    }
}
