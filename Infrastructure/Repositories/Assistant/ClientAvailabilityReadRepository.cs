// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed read-only repository for availability-gap scans. Soft-deleted clients,
/// memberships and availability rows are excluded by the global query filters.
/// </summary>

using Klacks.Api.Domain.DTOs.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class ClientAvailabilityReadRepository : IClientAvailabilityReadRepository
{
    private readonly DataBaseContext _context;

    public ClientAvailabilityReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<bool> AnyAvailabilityEntriesExistAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ClientAvailability.AnyAsync(cancellationToken);
    }

    public async Task<List<PlannableClientInfo>> GetPlannableClientsWithoutAvailabilityAsync(
        DateOnly fromInclusive,
        DateOnly untilInclusive,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var windowStart = fromInclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var windowEnd = untilInclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await _context.Client
            .AsNoTracking()
            .Where(c => c.Membership != null
                && c.Membership.ValidFrom <= windowEnd
                && (c.Membership.ValidUntil == null || c.Membership.ValidUntil >= windowStart))
            .Where(c => !_context.ClientAvailability.Any(a => a.ClientId == c.Id
                && a.Date >= fromInclusive
                && a.Date <= untilInclusive))
            .OrderBy(c => c.Name)
            .ThenBy(c => c.FirstName)
            .Select(c => new PlannableClientInfo(c.Id, c.FirstName, c.Name))
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}
