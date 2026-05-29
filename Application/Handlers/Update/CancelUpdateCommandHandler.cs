// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cancels a still-queued (Pending) update operation so the out-of-process updater never claims it.
/// Running operations cannot be cancelled here (they are mid-execution); returns false if the row is
/// missing or not Pending.
/// </summary>
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class CancelUpdateCommandHandler : IRequestHandler<CancelUpdateCommand, bool>
{
    private readonly IUpdateHistoryRepository _repository;

    public CancelUpdateCommandHandler(IUpdateHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(CancelUpdateCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null || entry.Status != UpdateOperationStatus.Pending)
        {
            return false;
        }

        entry.Status = UpdateOperationStatus.Cancelled;
        entry.CompletedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(entry, cancellationToken);
        return true;
    }
}
