// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IClientMembershipStartResolver"/>. Reads a client's Membership.ValidFrom directly;
/// returns null when the client has no membership row (e.g. an external/customer client type) so the
/// caller falls back to not clamping the window. The read for one client and the read for several clients use the
/// same query, the latter over chunks of IdListQueryLimits.ChunkSize ids.
/// </summary>
/// <param name="context">Database context providing the Client/Membership tables</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class ClientMembershipStartResolver : IClientMembershipStartResolver
{
    private readonly DataBaseContext _context;

    public ClientMembershipStartResolver(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<DateOnly?> GetValidFromAsync(Guid clientId)
    {
        var row = await QueryValidFrom([clientId]).FirstOrDefaultAsync();
        var validFrom = row?.ValidFrom;

        return validFrom.HasValue ? DateOnly.FromDateTime(validFrom.Value) : null;
    }

    public async Task<Dictionary<Guid, DateOnly?>> GetValidFromForClientsAsync(
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, DateOnly?>();
        foreach (var clientId in clientIds)
        {
            result[clientId] = null;
        }

        foreach (var chunk in result.Keys.ToList().Chunk(IdListQueryLimits.ChunkSize))
        {
            var rows = await QueryValidFrom(chunk).ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                result[row.ClientId] = row.ValidFrom.HasValue ? DateOnly.FromDateTime(row.ValidFrom.Value) : null;
            }
        }

        return result;
    }

    internal IQueryable<MembershipStartRow> QueryValidFrom(Guid[] clientIds)
        => _context.Client
            .AsNoTracking()
            .Where(c => clientIds.Contains(c.Id))
            .Select(c => new MembershipStartRow(c.Id, c.Membership != null ? c.Membership.ValidFrom : (DateTime?)null));

    internal sealed record MembershipStartRow(Guid ClientId, DateTime? ValidFrom);
}
