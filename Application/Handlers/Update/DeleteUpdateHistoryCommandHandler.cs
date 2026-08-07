// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes an update history row from the admin Updates card. Soft-deleting a row also hides it
/// from the single-active-operation guard (both the query filter and the partial unique index exclude
/// deleted rows), so a Running row is only removable once its updater lease has expired and no live
/// updater can still be executing it. Pending rows stay freely removable because the updater's claim
/// query skips deleted rows, so a deleted Pending row can never be picked up.
/// </summary>
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class DeleteUpdateHistoryCommandHandler : IRequestHandler<DeleteUpdateHistoryCommand, UpdateHistoryDeletionOutcome>
{
    private readonly IUpdateHistoryRepository _repository;

    public DeleteUpdateHistoryCommandHandler(IUpdateHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateHistoryDeletionOutcome> Handle(DeleteUpdateHistoryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
        {
            return UpdateHistoryDeletionOutcome.NotFound;
        }

        if (IsHeldByLiveUpdater(entry))
        {
            return UpdateHistoryDeletionOutcome.BlockedByLiveOperation;
        }

        await _repository.DeleteAsync(entry, cancellationToken);
        return UpdateHistoryDeletionOutcome.Deleted;
    }

    private static bool IsHeldByLiveUpdater(UpdateHistory entry) =>
        entry.Status == UpdateOperationStatus.Running
        && entry.LastHeartbeatAt is not null
        && DateTime.UtcNow - entry.LastHeartbeatAt.Value < UpdateOperationLease.StuckTimeout;
}
