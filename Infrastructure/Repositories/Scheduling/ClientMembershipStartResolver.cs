// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IClientMembershipStartResolver"/>. Reads a client's Membership.ValidFrom directly;
/// returns null when the client has no membership row (e.g. an external/customer client type) so the
/// caller falls back to not clamping the window.
/// </summary>
/// <param name="context">Database context providing the Client/Membership tables</param>

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
        var validFrom = await _context.Client
            .AsNoTracking()
            .Where(c => c.Id == clientId)
            .Select(c => c.Membership != null ? c.Membership.ValidFrom : (DateTime?)null)
            .FirstOrDefaultAsync();

        return validFrom.HasValue ? DateOnly.FromDateTime(validFrom.Value) : null;
    }
}
