// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Authentification;

public class IdentityProviderSyncLogRepository : BaseRepository<IdentityProviderSyncLog>, IIdentityProviderSyncLogRepository
{
    private readonly DataBaseContext context;

    public IdentityProviderSyncLogRepository(DataBaseContext context, ILogger<IdentityProviderSyncLog> logger)
        : base(context, logger)
    {
        this.context = context;
    }

    public async Task<IdentityProviderSyncLog?> GetByExternalId(Guid providerId, string externalId)
    {
        return await context.IdentityProviderSyncLogs
            .FirstOrDefaultAsync(s => s.IdentityProviderId == providerId && s.ExternalId == externalId);
    }

    public async Task<IdentityProviderSyncLog?> GetByExternalIdGlobal(string externalId)
    {
        return await context.IdentityProviderSyncLogs
            .FirstOrDefaultAsync(s => s.ExternalId == externalId);
    }

    public async Task<List<IdentityProviderSyncLog>> GetByProviderId(Guid providerId)
    {
        return await context.IdentityProviderSyncLogs
            .Where(s => s.IdentityProviderId == providerId)
            .ToListAsync();
    }

    public async Task<List<IdentityProviderSyncLog>> GetActiveByProviderId(Guid providerId)
    {
        return await context.IdentityProviderSyncLogs
            .Where(s => s.IdentityProviderId == providerId && s.IsActiveInSource)
            .ToListAsync();
    }
}
