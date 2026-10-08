// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the company membership window (Membership.ValidFrom / ValidUntil, inclusive) of a set of clients. A client
/// without a membership row gets no entry and counts as unrestricted, exactly like the schedule view does.
/// Single source of Wizard 1 (via <see cref="MembershipWindowReader"/>) and the harmonizer.
/// </summary>
/// <param name="context">EF Core context holding the memberships</param>
/// <param name="clientIds">Clients to look up</param>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

internal static class MembershipWindowQuery
{
    public static async Task<IReadOnlyDictionary<Guid, MembershipWindow>> LoadAsync(
        DataBaseContext context,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, MembershipWindow>();
        }

        var ids = clientIds.Distinct().ToList();
        var memberships = await context.Membership
            .AsNoTracking()
            .Where(m => ids.Contains(m.ClientId))
            .Select(m => new { m.ClientId, m.ValidFrom, m.ValidUntil })
            .ToListAsync(cancellationToken);

        return memberships
            .GroupBy(m => m.ClientId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var membership = g.First();
                    return new MembershipWindow(
                        DateOnly.FromDateTime(membership.ValidFrom),
                        membership.ValidUntil.HasValue ? DateOnly.FromDateTime(membership.ValidUntil.Value) : null);
                });
    }
}