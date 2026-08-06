// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a finished (non-active) update history row from the admin Updates card. Pending and
/// Running rows cannot be deleted here since the out-of-process updater is still tracking them.
/// </summary>
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class DeleteUpdateHistoryCommandHandler : IRequestHandler<DeleteUpdateHistoryCommand, bool>
{
    private static readonly UpdateOperationStatus[] ActiveStatuses =
        [UpdateOperationStatus.Pending, UpdateOperationStatus.Running];

    private readonly IUpdateHistoryRepository _repository;

    public DeleteUpdateHistoryCommandHandler(IUpdateHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteUpdateHistoryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null || ActiveStatuses.Contains(entry.Status))
        {
            return false;
        }

        await _repository.DeleteAsync(entry, cancellationToken);
        return true;
    }
}
