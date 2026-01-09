using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.IdentityProviders;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class IdentityProviderRepository : BaseRepository<IdentityProvider>, IIdentityProviderRepository
{
    private readonly DataBaseContext _context;
    private readonly ILdapService _ldapService;
    private readonly IClientSyncService _clientSyncService;

    public IdentityProviderRepository(
        DataBaseContext context,
        ILogger<IdentityProvider> logger,
        ILdapService ldapService,
        IClientSyncService clientSyncService)
        : base(context, logger)
    {
        _context = context;
        _ldapService = ldapService;
        _clientSyncService = clientSyncService;
    }

    public async Task<List<IdentityProvider>> GetEnabledProviders()
    {
        return await _context.IdentityProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<List<IdentityProvider>> GetAuthenticationProviders()
    {
        return await _context.IdentityProviders
            .Where(p => p.IsEnabled && p.UseForAuthentication)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<List<IdentityProvider>> GetClientImportProviders()
    {
        return await _context.IdentityProviders
            .Where(p => p.IsEnabled && p.UseForClientImport)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<TestConnectionResultResource> TestConnectionAsync(Guid providerId)
    {
        var provider = await Get(providerId);
        if (provider == null)
        {
            return new TestConnectionResultResource
            {
                Success = false,
                ErrorMessage = "Identity provider not found"
            };
        }

        return await _ldapService.TestConnectionAsync(provider);
    }

    public async Task<IdentityProviderSyncResultResource> SyncClientsAsync(Guid providerId)
    {
        var provider = await Get(providerId);
        if (provider == null)
        {
            return new IdentityProviderSyncResultResource
            {
                Success = false,
                ErrorMessage = "Identity provider not found",
                SyncTime = DateTime.UtcNow
            };
        }

        if (!provider.UseForClientImport)
        {
            return new IdentityProviderSyncResultResource
            {
                Success = false,
                ErrorMessage = "This provider is not configured for client import",
                SyncTime = DateTime.UtcNow
            };
        }

        return await _clientSyncService.SyncClientsAsync(provider);
    }
}
