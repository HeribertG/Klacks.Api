using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Interfaces;

public interface IIdentityProviderRepository : IBaseRepository<IdentityProvider>
{
    Task<List<IdentityProvider>> GetEnabledProviders();
    Task<List<IdentityProvider>> GetAuthenticationProviders();
    Task<List<IdentityProvider>> GetClientImportProviders();
}
