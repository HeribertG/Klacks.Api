// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Default implementation of IDayLockService delegating to ISealedDayRepository.
/// </summary>
/// <param name="repository">Source of SealedDay rows including group membership joins</param>
/// <param name="shiftRepository">Shift times, needed to place a work change on the day the schedule shows it</param>
public class DayLockService : IDayLockService
{
    private readonly ISealedDayRepository _repository;
    private readonly IShiftRepository _shiftRepository;

    public DayLockService(ISealedDayRepository repository, IShiftRepository shiftRepository)
    {
        _repository = repository;
        _shiftRepository = shiftRepository;
    }

    public async Task EnsureWorkChangeNotLockedAsync(Work parentWork, WorkChange change, CancellationToken cancellationToken = default)
    {
        if (parentWork.AnalyseToken.HasValue)
        {
            return;
        }

        var shift = await _shiftRepository.Get(parentWork.ShiftId);
        var day = shift is null
            ? parentWork.CurrentDate
            : WorkChangeEntryDay.Of(
                change.Type, parentWork.CurrentDate, parentWork.EndTime, change.StartTime, change.ChangeTime,
                shift.StartShift, shift.EndShift);

        var pairs = new List<(DateOnly Date, Guid ClientId)> { (day, parentWork.ClientId) };
        if (change.ReplaceClientId is { } replaceClientId)
        {
            pairs.Add((day, replaceClientId));
        }

        await ThrowWhenAnyLockedAsync(pairs, cancellationToken);
    }

    public Task EnsureDeletableAsync(
        IReadOnlyCollection<(DateOnly Date, Guid ClientId)> works,
        CancellationToken cancellationToken = default)
        => ThrowWhenAnyLockedAsync(works.Distinct().ToList(), cancellationToken);

    private async Task ThrowWhenAnyLockedAsync(IReadOnlyCollection<(DateOnly Date, Guid ClientId)> pairs, CancellationToken cancellationToken)
    {
        if (pairs.Count == 0)
        {
            return;
        }

        var locked = await _repository.GetLockedPairsAsync(pairs, cancellationToken);
        if (locked.Count > 0)
        {
            throw new InvalidRequestException(SealedDayMessage(locked.Min(p => p.Date)));
        }
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

    public async Task EnsureNotLockedForShiftAsync(DateOnly date, Guid shiftId, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        if (analyseToken.HasValue)
        {
            return;
        }

        if (await _repository.IsDayLockedForShiftAsync(date, shiftId, cancellationToken))
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
