// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Resolves a group's schedulable employees from <c>GroupItem</c> (the client→group membership),
/// restricted to memberships whose validity window overlaps the planning period. The soft-delete
/// query filter on GroupItem is honoured (no IgnoreQueryFilters), so retired memberships are excluded.
/// </summary>
/// <param name="context">EF context for the membership query</param>
public sealed class Wizard4AgentResolver : IWizard4AgentResolver
{
    private readonly DataBaseContext _context;

    public Wizard4AgentResolver(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> ResolveAsync(Guid groupId, DateOnly periodFrom, DateOnly periodUntil, CancellationToken ct)
    {
        // ValidFrom/ValidUntil are timestamptz; Npgsql requires UTC-kind DateTimes for them.
        var fromDt = periodFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var untilDt = periodUntil.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await _context.GroupItem
            .AsNoTracking()
            .Where(gi => gi.GroupId == groupId
                && gi.ClientId.HasValue
                && (gi.ValidFrom == null || gi.ValidFrom <= untilDt)
                && (gi.ValidUntil == null || gi.ValidUntil >= fromDt))
            .Select(gi => gi.ClientId!.Value)
            .Distinct()
            .ToListAsync(ct);
    }
}
