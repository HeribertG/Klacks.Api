// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes an update history row from the admin Updates card, including rows the out-of-process
/// updater still considers active (Pending/Running) so a stuck entry can be cleared manually.
/// </summary>
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class DeleteUpdateHistoryCommandHandler : IRequestHandler<DeleteUpdateHistoryCommand, bool>
{
    private readonly IUpdateHistoryRepository _repository;

    public DeleteUpdateHistoryCommandHandler(IUpdateHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteUpdateHistoryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        await _repository.DeleteAsync(entry, cancellationToken);
        return true;
    }
}
