// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed read-only repository for ClientContract rows.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class ClientContractReadRepository : IClientContractReadRepository
{
    private readonly DataBaseContext _context;

    public ClientContractReadRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<ClientContract>> GetExpiringBetweenAsync(DateOnly fromInclusive, DateOnly untilInclusive, CancellationToken cancellationToken = default)
    {
        return await _context.ClientContract
            .Include(c => c.Client)
            .Where(c => c.UntilDate != null
                && c.UntilDate >= fromInclusive
                && c.UntilDate <= untilInclusive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ClientContract>> GetContractsForClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.ClientContract
            .Where(c => c.ClientId == clientId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
