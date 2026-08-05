// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
            throw new InvalidRequestException(SealedDayMessage(date));
        }
    }

    public async Task EnsureNoneLockedAsync(
        IReadOnlyCollection<(DateOnly Date, Guid ClientId, Guid? AnalyseToken)> entries,
        CancellationToken cancellationToken = default)
    {
        var pairs = entries
            .Where(e => !e.AnalyseToken.HasValue)
            .Select(e => (e.Date, e.ClientId))
            .Distinct()
            .ToList();

        if (pairs.Count == 0)
        {
            return;
        }

        var locked = await _repository.GetLockedPairsAsync(pairs, cancellationToken);
        if (locked.Count == 0)
        {
            return;
        }

        throw new InvalidRequestException(SealedDayMessage(locked.Min(p => p.Date)));
    }

    private static string SealedDayMessage(DateOnly date)
        => $"Day {date:yyyy-MM-dd} is sealed and cannot be modified.";
}
