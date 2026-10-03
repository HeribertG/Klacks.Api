// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningConstraintReferenceReader"/>. Soft-deleted rows count as missing; the filters are
/// explicit so the result does not depend on which entities carry a global query filter.
/// </summary>
/// <param name="context">Database context holding groups, clients, scheduling rules and scenarios</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class PlanningConstraintReferenceReader : IPlanningConstraintReferenceReader
{
    private readonly DataBaseContext _context;

    public PlanningConstraintReferenceReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<bool> ScopeTargetExistsAsync(
        PlanningConstraintScopeType scopeType, Guid? scopeId, CancellationToken cancellationToken = default)
    {
        if (scopeType == PlanningConstraintScopeType.Global)
        {
            return true;
        }

        if (!scopeId.HasValue)
        {
            return false;
        }

        var id = scopeId.Value;
        return scopeType switch
        {
            PlanningConstraintScopeType.Group => await _context.Group.AnyAsync(g => g.Id == id && !g.IsDeleted, cancellationToken),
            PlanningConstraintScopeType.Client => await _context.Client.AnyAsync(c => c.Id == id && !c.IsDeleted, cancellationToken),
            PlanningConstraintScopeType.SchedulingRule => await _context.SchedulingRules.AnyAsync(r => r.Id == id && !r.IsDeleted, cancellationToken),
            _ => false,
        };
    }

    public async Task<bool> ActiveScenarioExistsAsync(Guid analyseToken, CancellationToken cancellationToken = default)
    {
        return await _context.AnalyseScenarios.AnyAsync(
            s => s.Token == analyseToken && !s.IsDeleted && s.Status == AnalyseScenarioStatus.Active,
            cancellationToken);
    }
}
