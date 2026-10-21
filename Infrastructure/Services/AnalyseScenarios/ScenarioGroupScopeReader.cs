// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the group scope of a scenario: the group hierarchy below a group and, for a clone, the shifts and employees
/// that hierarchy holds in the world the clone copies from.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Domain.Models.Associations;
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
    /// Employees of the group hierarchy: Real = the real members (the only employees whose real rows accept replaces);
    /// WithSource = additionally, with a source scenario, its temporary cross-group members (borrowed employees), whose
    /// rows OF THE SOURCE SCENARIO belong to the clone. Without a source both lists are the real members.
    /// </summary>
    internal async Task<(List<Guid> Real, List<Guid> WithSource)> LoadGroupClientScopeAsync(
        List<Guid> groupIds, ScenarioCloneSource source, CancellationToken ct)
    {
        var memberships = await ScenarioSourceFilters.WhereEitherWorld(_context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null), source)
            .Select(gi => new { ClientId = gi.ClientId!.Value, gi.AnalyseToken })
            .ToListAsync(ct);

        var real = memberships.Where(m => m.AnalyseToken == null).Select(m => m.ClientId).Distinct().ToList();
        var withSource = memberships.Select(m => m.ClientId).Distinct().ToList();
        return (real, withSource);
    }
}
