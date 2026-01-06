using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class IdentityProviderRepository : BaseRepository<IdentityProvider>, IIdentityProviderRepository
{
    private readonly DataBaseContext context;

    public IdentityProviderRepository(DataBaseContext context, ILogger<IdentityProvider> logger)
        : base(context, logger)
    {
        this.context = context;
    }

    public async Task<List<IdentityProvider>> GetEnabledProviders()
    {
        return await context.IdentityProviders
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<List<IdentityProvider>> GetAuthenticationProviders()
    {
        return await context.IdentityProviders
            .Where(p => p.IsEnabled && p.UseForAuthentication)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<List<IdentityProvider>> GetClientImportProviders()
    {
        return await context.IdentityProviders
            .Where(p => p.IsEnabled && p.UseForClientImport)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }
}
