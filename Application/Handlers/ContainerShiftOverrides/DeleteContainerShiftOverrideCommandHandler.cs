// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for deleting a container shift override ("Dienst zurücksetzen"). Fails if work exists. A missing or hidden
/// container is answered with 404; an override of another container is answered exactly like a missing override
/// (false), before the work check. Deliberately takes no container lock (the UI reset calls it without one).
/// </summary>
/// <param name="overrideId">The override to delete</param>
/// <param name="containerVisibilityGuard">Refuses missing and hidden containers alike</param>
using Klacks.Api.Application.Commands.ContainerShiftOverrides;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class DeleteContainerShiftOverrideCommandHandler : IRequestHandler<DeleteContainerShiftOverrideCommand, bool>
{
    private readonly IContainerShiftOverrideRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContainerVisibilityGuard _containerVisibilityGuard;
    private readonly ILogger<DeleteContainerShiftOverrideCommandHandler> _logger;

    public DeleteContainerShiftOverrideCommandHandler(
        IContainerShiftOverrideRepository repository,
        IUnitOfWork unitOfWork,
        IContainerVisibilityGuard containerVisibilityGuard,
        ILogger<DeleteContainerShiftOverrideCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _containerVisibilityGuard = containerVisibilityGuard;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteContainerShiftOverrideCommand request, CancellationToken cancellationToken)
    {
        await _containerVisibilityGuard.EnsureContainerVisibleAsync(request.ContainerId, cancellationToken);

        var existing = await _repository.GetWithTracking(request.OverrideId, cancellationToken);
        if (existing is null || existing.ContainerId != request.ContainerId) return false;

        var hasWork = await _repository.HasWorkForOverride(existing.ContainerId, existing.Date, cancellationToken);
        if (hasWork)
        {
            throw new ConflictException("Cannot delete override: work entries already exist for this date.");
        }

        await _repository.Delete(existing);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Deleted ContainerShiftOverride {OverrideId}", request.OverrideId);

        return true;
    }
}
