// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Takes a move back: the single place where going back to the origin clears Work.MovedFromClientId and stamps the book.
/// </summary>
/// <param name="repository">Finds the applied Move row of the step</param>
/// <param name="timeProvider">Supplies the instant of the reversal</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class MovedWorkReversal : IMovedWorkReversal
{
    private readonly IReplacementRequestRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MovedWorkReversal(IReplacementRequestRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task StageAsync(Work work, Guid targetClientId, CancellationToken cancellationToken)
    {
        if (work.MovedFromClientId != targetClientId)
        {
            return;
        }

        var row = await _repository.FindAppliedMoveAsync(work.Id, targetClientId, work.ClientId, cancellationToken);
        work.MovedFromClientId = null;
        if (row is not null)
        {
            row.ReversedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        }
    }
}
