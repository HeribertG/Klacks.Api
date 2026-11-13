// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the group scope of a scenario: the group hierarchy below a group and, for a clone, the shifts and employees
/// that hierarchy holds in the world the clone copies from. The real members are read in one place
/// (LoadRealGroupClientIdsAsync) for the clone and for the accept, so both look at the same people: the regular members.
/// A person who was only borrowed for single days is no member here; her real breaks and notes belong to the scenarios
/// of her home group, and her works on the shifts of this group come through the shift scope.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioGroupScopeReader
{
    private readonly DataBaseContext _context;

    public ScenarioGroupScopeReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetGroupHierarchyIdsAsync(Guid groupId, CancellationToken ct)
    {
        var allGroups = await _context.Group
            .Where(g => !g.IsDeleted)
            .Select(g => new { g.Id, g.Parent })
            .ToListAsync(ct);

        var result = new List<Guid> { groupId };
        var queue = new Queue<Guid>();
        queue.Enqueue(groupId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            foreach (var childId in allGroups.Where(g => g.Parent == currentId).Select(g => g.Id))
            {
                result.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return result;
    }

    /// <summary>
    /// Shifts of the group hierarchy: the real memberships, plus with a source scenario the memberships of its clone
    /// shifts (a source clone stands in for its real root, see ScenarioShiftCloner.LoadShiftsToCloneAsync).
    /// </summary>
    internal async Task<List<Guid>> LoadGroupShiftIdsAsync(List<Guid> groupIds, ScenarioCloneSource source, CancellationToken ct)
        => await ScenarioSourceFilters.WhereEitherWorld(_context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null), source)
            .Select(gi => gi.ShiftId!.Value)
            .Distinct()
            .ToListAsync(ct);

    /// <summary>
    /// Employees of the group hierarchy: Real = the regular members (the only employees whose real rows accept replaces);
    /// WithSource = additionally, with a source scenario, its temporary cross-group members (borrowed employees), whose
    /// rows OF THE SOURCE SCENARIO belong to the clone. Without a source both lists are the regular members.
    /// </summary>
    internal async Task<(List<Guid> Real, List<Guid> WithSource)> LoadGroupClientScopeAsync(
        List<Guid> groupIds, ScenarioCloneSource source, CancellationToken ct)
    {
        var real = await LoadRealGroupClientIdsAsync(groupIds, ct);
        if (source.Token is not { } sourceToken)
        {
            return (real, real);
        }

        var borrowedInSource = await _context.Set<GroupItem>()
            .Where(gi => !gi.IsDeleted && gi.AnalyseToken == sourceToken && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
            .Select(gi => gi.ClientId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return (real, real.Union(borrowedInSource).ToList());
    }

    /// <summary>
    /// The real members of the group hierarchy: the people with a regular membership. Temporary memberships of borrowed
    /// days do not count, whatever their day: neither a clone copies nor an accept replaces the real breaks and notes
    /// of a borrowed person.
    /// </summary>
    /// <param name="groupIds">Groups of the hierarchy</param>
    /// <param name="ct">Cancellation token</param>
    internal async Task<List<Guid>> LoadRealGroupClientIdsAsync(List<Guid> groupIds, CancellationToken ct)
        => await _context.Set<GroupItem>()
            .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null)
            .Where(RegularMembership.IsRegular)
            .Select(gi => gi.ClientId!.Value)
            .Distinct()
            .ToListAsync(ct);
}
