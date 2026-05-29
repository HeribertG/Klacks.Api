// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enqueues a rollback of the most recent successful update by writing a single Pending hand-off row.
/// Targets the version that preceded that update and links the row to it via RelatedOperationId so the
/// out-of-process updater can restore the paired pre-migration backup. Rejects when an operation is
/// already active or there is no successful update to undo.
/// </summary>
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Update;

public class RequestRollbackCommandHandler : IRequestHandler<RequestRollbackCommand, UpdateTriggerResult>
{
    private readonly IUpdateHistoryRepository _repository;

    public RequestRollbackCommandHandler(IUpdateHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateTriggerResult> Handle(RequestRollbackCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.GetActiveOperationAsync(cancellationToken) is not null)
        {
            return Rejected(UpdateReasons.OperationInProgress);
        }

        var lastUpdate = await _repository.GetLastSuccessfulUpdateAsync(cancellationToken);
        if (lastUpdate is null)
        {
            return Rejected(UpdateReasons.NothingToRollback);
        }

        var entry = new UpdateHistory
        {
            Id = Guid.NewGuid(),
            OperationType = UpdateOperationType.Rollback,
            Status = UpdateOperationStatus.Pending,
            Channel = lastUpdate.Channel,
            FromVersion = lastUpdate.TargetVersion,
            TargetVersion = lastUpdate.FromVersion,
            RelatedOperationId = lastUpdate.Id,
            BackupRef = lastUpdate.BackupRef,
            ContainsMigrations = lastUpdate.ContainsMigrations,
            RequestedBy = request.RequestedBy,
            RequestedAt = DateTime.UtcNow,
        };

        try
        {
            await _repository.AddAsync(entry, cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Rejected(UpdateReasons.OperationInProgress);
        }

        return new UpdateTriggerResult
        {
            Enqueued = true,
            OperationId = entry.Id,
            Reason = UpdateReasons.RollbackEnqueued,
        };
    }

    private static UpdateTriggerResult Rejected(string reason) => new() { Enqueued = false, Reason = reason };
}
