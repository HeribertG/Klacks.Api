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
/// <param name="workChangeLockedWorkIds">Works (real or scenario) with a work change shown on a locked day, for the
/// work's employee or a replacement employee (including the post-midnight part shown on the following day): the work
/// must not be written or removed</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed class AcceptDayLockPlan
{
    private readonly HashSet<(Guid ClientId, DateOnly Date)> _lockedDays;
    private readonly HashSet<Guid> _keptRealWorkIds;
    private readonly HashSet<Guid> _discardedCloneWorkIds;
    private readonly IReadOnlyDictionary<Guid, AcceptWorkSnapshot> _realOriginByCloneId;
    private readonly HashSet<Guid> _workChangeLockedWorkIds;

    public AcceptDayLockPlan(
        HashSet<(Guid ClientId, DateOnly Date)> lockedDays,
        HashSet<Guid> keptRealWorkIds,
        HashSet<Guid> discardedCloneWorkIds,
        IReadOnlyDictionary<Guid, AcceptWorkSnapshot> realOriginByCloneId,
        HashSet<Guid> workChangeLockedWorkIds)
    {
        _lockedDays = lockedDays;
        _keptRealWorkIds = keptRealWorkIds;
        _discardedCloneWorkIds = discardedCloneWorkIds;
        _realOriginByCloneId = realOriginByCloneId;
        _workChangeLockedWorkIds = workChangeLockedWorkIds;
    }

    public bool IsWorkChangeLocked(Guid workId) => _workChangeLockedWorkIds.Contains(workId);

    public IReadOnlyCollection<Guid> KeptRealWorkIds => _keptRealWorkIds;

    public IReadOnlyCollection<Guid> DiscardedCloneWorkIds => _discardedCloneWorkIds;

    public bool IsLocked(Guid clientId, DateOnly date) => _lockedDays.Contains((clientId, date));

    public bool KeepsRealWork(Guid workId) => _keptRealWorkIds.Contains(workId);

    /// <summary>
    /// True when the accept leaves this real work (and its work changes, expenses and pauses) in place: it is locked,
    /// sits on a locked day, has a work change shown on a locked day, or its slot is kept.
    /// </summary>
    public bool LeavesRealWork(Guid workId, Guid clientId, DateOnly date, WorkLockLevel lockLevel)
        => lockLevel != WorkLockLevel.None
            || IsLocked(clientId, date)
            || IsWorkChangeLocked(workId)
            || KeepsRealWork(workId);

    /// <summary>
    /// True when the accept leaves this real break in place: it is locked, sits on a locked day, or pauses a real work
    /// the accept keeps. The one rule shared by the delete, the staleness check and the skip count.
    /// </summary>
    /// <param name="keptWorkIds">Real works the accept keeps (as decided by LeavesRealWork for the delete's scope)</param>
    public bool LeavesRealBreak(
        WorkLockLevel lockLevel, Guid clientId, DateOnly date, Guid? parentWorkId, IReadOnlySet<Guid> keptWorkIds)
        => lockLevel != WorkLockLevel.None
            || IsLocked(clientId, date)
            || (parentWorkId is { } parentId && keptWorkIds.Contains(parentId));

    public bool DiscardsClone(Guid workId) => _discardedCloneWorkIds.Contains(workId);

    public AcceptWorkSnapshot? RealOriginOf(Guid cloneWorkId)
        => _realOriginByCloneId.TryGetValue(cloneWorkId, out var origin) ? origin : null;
}
