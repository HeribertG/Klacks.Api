// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cancels a still-queued (Pending) update operation so the out-of-process updater never claims it.
/// Running operations cannot be cancelled here (they are mid-execution). Cancelled is a terminal
/// status that satisfies neither the single-active guard nor its partial unique index, so writing it
/// over a row the updater has meanwhile claimed would retire both while the operation keeps running.
/// The status is a concurrency token, which turns that race into a failed write the caller is told
/// about rather than a silent overwrite.
/// </summary>
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Update;

public class CancelUpdateCommandHandler : IRequestHandler<CancelUpdateCommand, UpdateOperationCancellationOutcome>
{
    private readonly IUpdateHistoryRepository _repository;

    public CancelUpdateCommandHandler(IUpdateHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateOperationCancellationOutcome> Handle(CancelUpdateCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null)
        {
            return UpdateOperationCancellationOutcome.NotFound;
        }

        if (entry.Status != UpdateOperationStatus.Pending)
        {
            return UpdateOperationCancellationOutcome.NoLongerPending;
        }

        entry.Status = UpdateOperationStatus.Cancelled;
        entry.CompletedAt = DateTime.UtcNow;

        try
        {
            await _repository.UpdateAsync(entry, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return UpdateOperationCancellationOutcome.NoLongerPending;
        }

        return UpdateOperationCancellationOutcome.Cancelled;
    }
}
