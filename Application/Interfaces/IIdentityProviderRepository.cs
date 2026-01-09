using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Interfaces;

public interface IIdentityProviderRepository : IBaseRepository<IdentityProvider>
{
    Task<List<IdentityProvider>> GetEnabledProviders();
    Task<List<IdentityProvider>> GetAuthenticationProviders();
    Task<List<IdentityProvider>> GetClientImportProviders();
    Task<TestConnectionResultResource> TestConnectionAsync(Guid providerId);
    Task<IdentityProviderSyncResultResource> SyncClientsAsync(Guid providerId);
}
