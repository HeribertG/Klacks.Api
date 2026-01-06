using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Interfaces;

public interface IIdentityProviderSyncLogRepository : IBaseRepository<IdentityProviderSyncLog>
{
    Task<IdentityProviderSyncLog?> GetByExternalId(Guid providerId, string externalId);
    Task<List<IdentityProviderSyncLog>> GetByProviderId(Guid providerId);
    Task<List<IdentityProviderSyncLog>> GetActiveByProviderId(Guid providerId);
}
