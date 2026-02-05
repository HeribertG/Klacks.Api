using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IClientSyncService
{
    Task<IdentityProviderSyncResultResource> SyncClientsAsync(IdentityProvider provider);
}
