// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Default implementation of IDayLockService delegating to ISealedDayRepository.
/// </summary>
/// <param name="repository">Source of SealedDay rows including group membership joins</param>
public class DayLockService : IDayLockService
{
    private readonly ISealedDayRepository _repository;

    public DayLockService(ISealedDayRepository repository)
    {
        _repository = repository;
    }

    public async Task EnsureNotLockedAsync(DateOnly date, Guid clientId, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        if (analyseToken.HasValue)
        {
            return;
        }

        if (await _repository.IsDayLockedAsync(date, clientId, cancellationToken))
        {
            throw new InvalidRequestException($"Day {date:yyyy-MM-dd} is sealed and cannot be modified.");
        }
    }
}
