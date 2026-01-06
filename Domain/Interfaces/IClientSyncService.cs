using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Domain.Interfaces;

public interface IClientSyncService
{
    Task<IdentityProviderSyncResultResource> SyncClientsAsync(IdentityProvider provider);
}
