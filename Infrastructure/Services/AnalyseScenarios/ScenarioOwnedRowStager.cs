// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scenario-owned rows that are not cloned from the real plan: temporary cross-group memberships and
/// scenario-scoped planning constraints. Planning constraints of a scenario are soft-deleted on reject/delete
/// AND on accept - accepting a scenario adopts its plan, never its what-if rules: promoting them to real rules
/// would approve a rule outside the UI pending list (owner decision 2026-10-03).
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioOwnedRowStager
{
    private readonly DataBaseContext _context;

    public ScenarioOwnedRowStager(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddScenarioMembershipAsync(
        Guid token, Guid clientId, Guid groupId, DateOnly validFrom, DateOnly validUntil, CancellationToken cancellationToken)
    {
        var membership = new GroupItem
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            GroupId = groupId,
            ShiftId = null,
            ValidFrom = validFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            ValidUntil = validUntil.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            AnalyseToken = token
        };
        await _context.Set<GroupItem>().AddAsync(membership, cancellationToken);
    }

    public async Task SoftDeleteScenarioPlanningConstraintsAsync(Guid token, DateTime nowUtc, CancellationToken ct)
    {
        var constraints = await _context.PlanningConstraint
            .Where(c => c.AnalyseToken == token && !c.IsDeleted)
            .ToListAsync(ct);
        foreach (var constraint in constraints)
        {
            constraint.IsDeleted = true;
            constraint.DeletedTime = nowUtc;
        }
    }
}
