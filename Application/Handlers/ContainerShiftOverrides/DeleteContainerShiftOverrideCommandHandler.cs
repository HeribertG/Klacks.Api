// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for deleting a container shift override ("Dienst zurücksetzen"). Fails if work exists.
/// </summary>
/// <param name="overrideId">The override to delete</param>
using Klacks.Api.Application.Commands.ContainerShiftOverrides;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class DeleteContainerShiftOverrideCommandHandler : IRequestHandler<DeleteContainerShiftOverrideCommand, bool>
{
    private readonly IContainerShiftOverrideRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteContainerShiftOverrideCommandHandler> _logger;

    public DeleteContainerShiftOverrideCommandHandler(
        IContainerShiftOverrideRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteContainerShiftOverrideCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteContainerShiftOverrideCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetWithTracking(request.OverrideId, cancellationToken);
        if (existing is null) return false;

        var hasWork = await _repository.HasWorkForOverride(existing.ContainerId, existing.Date, cancellationToken);
        if (hasWork)
        {
            throw new Exception("Cannot delete override: work entries already exist for this date.");
        }

        await _repository.Delete(existing);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Deleted ContainerShiftOverride {OverrideId}", request.OverrideId);

        return true;
    }
}
