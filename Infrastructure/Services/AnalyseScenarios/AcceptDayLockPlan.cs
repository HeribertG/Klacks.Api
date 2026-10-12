// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What an accept leaves untouched because of the day lock, decided once from the database state before anything is
/// staged: the locked (client, day) pairs of the period, the real works that must survive (they sit on a locked day or
/// are locked, or one of their scenario clones does) and the scenario work clones that must not be promoted. A slot is
/// treated as a unit: when either side of a scenario edit touches a locked day, the real work stays and every clone of
/// it is discarded, so an accept never applies half a move (no duplicate, no lost slot).
/// </summary>
/// <param name="lockedDays">Locked (client, day) pairs of the accept period</param>
/// <param name="keptRealWorkIds">Real works the accept must not delete</param>
/// <param name="discardedCloneWorkIds">Scenario works the accept must not promote</param>
/// <param name="realOriginByCloneId">Live real work each scenario clone stands for, when it has one</param>

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed class AcceptDayLockPlan
{
    private readonly HashSet<(Guid ClientId, DateOnly Date)> _lockedDays;
    private readonly HashSet<Guid> _keptRealWorkIds;
    private readonly HashSet<Guid> _discardedCloneWorkIds;
    private readonly IReadOnlyDictionary<Guid, AcceptWorkSnapshot> _realOriginByCloneId;

    public AcceptDayLockPlan(
        HashSet<(Guid ClientId, DateOnly Date)> lockedDays,
        HashSet<Guid> keptRealWorkIds,
        HashSet<Guid> discardedCloneWorkIds,
        IReadOnlyDictionary<Guid, AcceptWorkSnapshot> realOriginByCloneId)
    {
        _lockedDays = lockedDays;
        _keptRealWorkIds = keptRealWorkIds;
        _discardedCloneWorkIds = discardedCloneWorkIds;
        _realOriginByCloneId = realOriginByCloneId;
    }

    public IReadOnlyCollection<Guid> KeptRealWorkIds => _keptRealWorkIds;

    public IReadOnlyCollection<Guid> DiscardedCloneWorkIds => _discardedCloneWorkIds;

    public bool IsLocked(Guid clientId, DateOnly date) => _lockedDays.Contains((clientId, date));

    public bool KeepsRealWork(Guid workId) => _keptRealWorkIds.Contains(workId);

    public bool DiscardsClone(Guid workId) => _discardedCloneWorkIds.Contains(workId);

    public AcceptWorkSnapshot? RealOriginOf(Guid cloneWorkId)
        => _realOriginByCloneId.TryGetValue(cloneWorkId, out var origin) ? origin : null;
}
