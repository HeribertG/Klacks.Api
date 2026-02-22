// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class HeartbeatConfigRepository : IHeartbeatConfigRepository
{
    private readonly DataBaseContext _context;

    public HeartbeatConfigRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<HeartbeatConfig?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.HeartbeatConfigs
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<List<HeartbeatConfig>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.HeartbeatConfigs
            .Where(c => c.IsEnabled)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(HeartbeatConfig config, CancellationToken cancellationToken = default)
    {
        await _context.HeartbeatConfigs.AddAsync(config, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(HeartbeatConfig config, CancellationToken cancellationToken = default)
    {
        _context.HeartbeatConfigs.Update(config);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
