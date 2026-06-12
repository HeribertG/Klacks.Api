// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for dynamically registered OAuth clients (RFC 7591) backed by the oauth_clients table.
/// </summary>
/// <param name="context">Database context used for client lookup and persistence</param>

using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Authentification;

public class OAuthClientRepository : IOAuthClientRepository
{
    private readonly DataBaseContext _context;

    public OAuthClientRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<OAuthClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.OAuthClients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task AddAsync(OAuthClient client, CancellationToken cancellationToken = default)
    {
        await _context.OAuthClients.AddAsync(client, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
