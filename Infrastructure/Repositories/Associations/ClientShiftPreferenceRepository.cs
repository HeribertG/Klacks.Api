// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for managing client shift preferences with shift include support.
/// </summary>
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class ClientShiftPreferenceRepository : BaseRepository<ClientShiftPreference>, IClientShiftPreferenceRepository
{
    private readonly DataBaseContext context;

    public ClientShiftPreferenceRepository(DataBaseContext context, ILogger<ClientShiftPreference> logger)
        : base(context, logger)
    {
        this.context = context;
    }

    public async Task<List<ClientShiftPreference>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default)
    {
        return await context.ClientShiftPreference
            .Include(csp => csp.Shift)
            .Where(csp => csp.ClientId == clientId)
            .OrderBy(csp => csp.PreferenceType)
            .ThenBy(csp => csp.Shift!.Name)
            .ToListAsync(ct);
    }

    public async Task DeleteAllByClientIdAsync(Guid clientId, CancellationToken ct = default)
    {
        var existing = await context.ClientShiftPreference
            .Where(csp => csp.ClientId == clientId)
            .ToListAsync(ct);

        context.ClientShiftPreference.RemoveRange(existing);
    }
}
