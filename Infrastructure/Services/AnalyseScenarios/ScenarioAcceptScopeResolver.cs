// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The real shifts whose works and the real clients whose breaks and notes an accept replaces. Shared by the accept delete
/// and the staleness check, so both always look at exactly the same rows. With a group it is the group hierarchy's real
/// members; without one it is the scenario's own footprint.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="groupScopeReader">Resolves the group hierarchy of the scenario's group</param>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioAcceptScopeResolver
{
    private readonly DataBaseContext _context;
    private readonly ScenarioGroupScopeReader _groupScopeReader;

    public ScenarioAcceptScopeResolver(DataBaseContext context, ScenarioGroupScopeReader groupScopeReader)
    {
        _context = context;
        _groupScopeReader = groupScopeReader;
    }

    public async Task<(List<Guid>? ShiftIds, List<Guid>? ClientIds)> ResolveRealAcceptScopeAsync(
        Guid? groupId, Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
        => groupId.HasValue
            ? await ResolveGroupScopeAsync(groupId.Value, ct)
            : await ResolveScenarioFootprintAsync(token, fromDate, untilDate, ct);

    private async Task<(List<Guid>? ShiftIds, List<Guid>? ClientIds)> ResolveGroupScopeAsync(Guid groupId, CancellationToken ct)
    {
        var groupIds = await _groupScopeReader.GetGroupHierarchyIdsAsync(groupId, ct);

        var shiftIds = await _context.Set<GroupItem>()
            .Where(gi => !gi.IsDeleted && gi.AnalyseToken == null
                && groupIds.Contains(gi.GroupId) && gi.ShiftId != null)
            .Select(gi => gi.ShiftId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var clientIds = await _context.Set<GroupItem>()
            .Where(gi => !gi.IsDeleted && gi.AnalyseToken == null
                && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
            .Select(gi => gi.ClientId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return (shiftIds, clientIds);
    }

    /// <summary>
    /// No group on the scenario: scope the delete to the scenario's OWN footprint - the real shifts its works will promote
    /// onto, and the clients its works, breaks and notes touch - instead of deleting the whole date window company-wide.
    /// An empty footprint deletes nothing.
    /// </summary>
    private async Task<(List<Guid>? ShiftIds, List<Guid>? ClientIds)> ResolveScenarioFootprintAsync(
        Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var cloneToSource = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId.HasValue)
            .Select(s => new { s.Id, SourceId = s.ScenarioSourceShiftId!.Value })
            .ToDictionaryAsync(s => s.Id, s => s.SourceId, ct);

        var scenarioWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted
                && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate)
            .Select(w => new { w.ShiftId, w.ClientId })
            .ToListAsync(ct);

        var shiftIds = scenarioWorks
            .Select(w => cloneToSource.TryGetValue(w.ShiftId, out var src) ? src : w.ShiftId)
            .Distinct()
            .ToList();

        var breakClientIds = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted
                && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate)
            .Select(b => b.ClientId)
            .ToListAsync(ct);

        var noteClientIds = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted
                && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate)
            .Select(sn => sn.ClientId)
            .ToListAsync(ct);

        var clientIds = scenarioWorks.Select(w => w.ClientId)
            .Concat(breakClientIds)
            .Concat(noteClientIds)
            .Distinct()
            .ToList();

        return (shiftIds, clientIds);
    }
}
