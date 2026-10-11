// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Source selection of a scenario clone (scenario from scenario): resolves and guards the source scenario, maps clone
/// shifts to their real root, and copies the rows only a source scenario has (sub-breaks reached through their cloned
/// parent work, temporary memberships, open replacement requests). The child is accepted straight into the real plan;
/// its source stays an independent scenario that can still be accepted or rejected like any sibling scenario, with the
/// usual accept conflict checks.
/// </summary>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public partial class AnalyseScenarioService
{
    private const string SourceScenarioNotFoundMessage = "Source scenario with token {0} not found.";

    private const string SourceScenarioNotActiveMessage =
        "Source scenario {0} is no longer active (status {1}); a new scenario can only be built on an active scenario.";

    private const string SourceScenarioGroupScopeMessage =
        "Source scenario {0} only covers group {1} and its subgroups; a scenario built on it must stay inside that group.";

    private const string RootlessScenarioShiftMessage =
        "Cannot accept scenario: works are planned on scenario shifts that have no shift in the original plan: {0}.";

    /// <summary>
    /// Loads the source scenario and refuses one that cannot carry a child: a missing or deleted scenario, a decided one
    /// (its rows are promoted or discarded, so the clone would be empty and accepting it would wipe the real plan) and a
    /// group outside the source scenario's group hierarchy (the source holds no rows for it). A child without a group on a
    /// grouped source inherits the source's group. No visibility check: the caller must have verified that the user may
    /// see the source scenario's group (CoverAbsence does; the wizard and harmonizer apply paths are admin-only).
    /// </summary>
    /// <param name="sourceToken">Token of the scenario to build on; null = the real plan</param>
    /// <param name="groupId">Group of the new scenario; null = unscoped</param>
    /// <returns>The clone source and the group that scopes the clone</returns>
    private async Task<(ScenarioCloneSource Source, Guid? ScopeGroupId)> ResolveCloneSourceAsync(
        Guid? sourceToken, Guid? groupId, CancellationToken ct)
    {
        if (sourceToken is not { } token)
        {
            return (ScenarioCloneSource.RealPlan, groupId);
        }

        var source = await _context.Set<AnalyseScenario>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == token, ct)
            ?? throw new KeyNotFoundException(string.Format(SourceScenarioNotFoundMessage, token));

        if (source.Status != AnalyseScenarioStatus.Active)
        {
            throw new ScenarioNotActiveException(string.Format(SourceScenarioNotActiveMessage, source.Id, source.Status));
        }

        var scopeGroupId = groupId ?? source.GroupId;
        if (source.GroupId is { } sourceGroupId)
        {
            var sourceGroupIds = await GetGroupHierarchyIdsAsync(sourceGroupId, ct);
            if (!sourceGroupIds.Contains(scopeGroupId!.Value))
            {
                throw new ConflictException(string.Format(SourceScenarioGroupScopeMessage, source.Id, sourceGroupId));
            }
        }

        return (new ScenarioCloneSource(token, source.FromDate, source.UntilDate), scopeGroupId);
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> ResolveRootShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken)
    {
        if (shiftIds.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var ids = shiftIds.Distinct().ToList();
        return await _context.Shift.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id) && s.ScenarioSourceShiftId != null)
            .ToDictionaryAsync(s => s.Id, s => s.ScenarioSourceShiftId!.Value, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetScenarioShiftIdsWithRootsAsync(Guid token, CancellationToken cancellationToken)
    {
        var shifts = await _context.Shift.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted)
            .Select(s => new { s.Id, s.ScenarioSourceShiftId })
            .ToListAsync(cancellationToken);

        var ids = shifts.Select(s => s.Id).ToHashSet();
        ids.UnionWith(shifts.Where(s => s.ScenarioSourceShiftId.HasValue).Select(s => s.ScenarioSourceShiftId!.Value));
        return ids;
    }

    /// <summary>
    /// Refuses to accept a scenario whose works sit on a scenario shift without a real root: promote would leave those
    /// works on a shift it soft-deletes. No current write path creates such a shift; the guard keeps it a clean 409
    /// instead of orphaned real works should one appear.
    /// </summary>
    private async Task EnsureNoRootlessScenarioShiftInUseAsync(Guid token, CancellationToken ct)
    {
        var rootlessShiftIds = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId == null)
            .Select(s => s.Id)
            .ToListAsync(ct);
        if (rootlessShiftIds.Count == 0)
        {
            return;
        }

        var usedShiftIds = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted && rootlessShiftIds.Contains(w.ShiftId))
            .Select(w => w.ShiftId)
            .Distinct()
            .ToListAsync(ct);
        if (usedShiftIds.Count > 0)
        {
            throw new ConflictException(string.Format(RootlessScenarioShiftMessage, string.Join(", ", usedShiftIds)));
        }
    }

    /// <summary>
    /// Breaks of a scenario-sourced clone. Standalone breaks (absences) follow the date split of the source range and the
    /// employee scope; sub-breaks (work pauses) follow their cloned parent work, exactly like sub-works, so a night work at
    /// the source range's edge keeps the pause that falls on the next day.
    /// </summary>
    private async Task<List<Break>> LoadSourcedBreaksAsync(
        List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, Dictionary<Guid, Guid> workIdMap,
        ScenarioCloneSource source, CancellationToken ct)
    {
        var standaloneQuery = WhereFromSource(_context.Break
            .Where(b => !b.IsDeleted
                && b.ParentWorkId == null
                && b.CurrentDate >= fromDate
                && b.CurrentDate <= untilDate), source);

        if (groupIds != null)
        {
            standaloneQuery = WhereClientInScope(standaloneQuery, await LoadGroupClientScopeAsync(groupIds, source, ct), source);
        }

        var breaks = await standaloneQuery.AsNoTracking().ToListAsync(ct);

        var parentWorkIds = workIdMap.Keys.ToList();
        if (parentWorkIds.Count > 0)
        {
            breaks.AddRange(await WhereEitherWorld(_context.Break
                    .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && parentWorkIds.Contains(b.ParentWorkId.Value)), source)
                .AsNoTracking()
                .ToListAsync(ct));
        }

        return breaks;
    }

    /// <summary>
    /// The real root shift a cloned shift stands for: a real shift is its own root, a scenario shift carries its root in
    /// ScenarioSourceShiftId (null only for a shift that exists in a scenario alone, which then has no root to land on).
    /// </summary>
    private static Guid? RootShiftIdOf(Shift shift)
        => shift.AnalyseToken is null ? shift.Id : shift.ScenarioSourceShiftId;

    /// <summary>
    /// Adds root aliases to the shift map of a scenario-sourced clone: a real work taken from outside the source range
    /// points at the real root shift, which the clone represents by the copy of the source scenario's clone.
    /// </summary>
    private static Dictionary<Guid, Guid> WithRootAliases(
        IReadOnlyCollection<Shift> clonedShifts, Dictionary<Guid, Guid> shiftIdMap, ScenarioCloneSource source)
    {
        if (source.Token is null)
        {
            return shiftIdMap;
        }

        var aliased = new Dictionary<Guid, Guid>(shiftIdMap);
        foreach (var shift in clonedShifts)
        {
            if (shift.AnalyseToken is not null && shift.ScenarioSourceShiftId is { } rootId)
            {
                aliased.TryAdd(rootId, shiftIdMap[shift.Id]);
            }
        }

        return aliased;
    }

    /// <summary>
    /// Copies the source scenario's temporary cross-group memberships (borrowed employees) into the new scenario, so a
    /// cover that relies on them survives into the child and is promoted with it on accept.
    /// </summary>
    private async Task CloneScenarioMemberships(ScenarioCloneSource source, Guid token, CancellationToken ct)
    {
        if (source.Token is not { } sourceToken)
        {
            return;
        }

        var memberships = await _context.Set<GroupItem>()
            .Where(gi => !gi.IsDeleted && gi.AnalyseToken == sourceToken && gi.ShiftId == null && gi.ClientId != null)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var membership in memberships)
        {
            await _context.Set<GroupItem>().AddAsync(new GroupItem
            {
                Id = Guid.NewGuid(),
                ClientId = membership.ClientId,
                GroupId = membership.GroupId,
                ShiftId = null,
                ValidFrom = membership.ValidFrom,
                ValidUntil = membership.ValidUntil,
                AnalyseToken = token
            }, ct);
        }
    }

    /// <summary>
    /// Copies the source scenario's not yet applied replacement requests (request book) into the new scenario, so
    /// accepting the child stamps them applied. ShiftId is already the real root shift; WorkChangeId pointed at the source
    /// scenario's clone and is left for accept to fill. The source's own rows stay untouched.
    /// </summary>
    private async Task CloneOpenReplacementRequests(ScenarioCloneSource source, Guid token, CancellationToken ct)
    {
        if (source.Token is not { } sourceToken)
        {
            return;
        }

        var requests = await _context.ReplacementRequests
            .Where(r => r.AnalyseToken == sourceToken && r.AppliedAtUtc == null)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var request in requests)
        {
            await _context.ReplacementRequests.AddAsync(new ReplacementRequest
            {
                Id = Guid.NewGuid(),
                AbsentClientId = request.AbsentClientId,
                CandidateClientId = request.CandidateClientId,
                ShiftId = request.ShiftId,
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                GroupId = request.GroupId,
                AbsenceId = request.AbsenceId,
                Source = request.Source,
                Outcome = request.Outcome,
                ReportedAtUtc = request.ReportedAtUtc,
                ShiftStartUtc = request.ShiftStartUtc,
                OutcomeAtUtc = request.OutcomeAtUtc,
                OutcomeByUserId = request.OutcomeByUserId,
                AnalyseToken = token
            }, ct);
        }
    }
}
