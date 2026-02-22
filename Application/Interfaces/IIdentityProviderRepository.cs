// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Interfaces;

public interface IIdentityProviderRepository : IBaseRepository<IdentityProvider>
{
    Task<List<IdentityProvider>> GetEnabledProviders();
    Task<List<IdentityProvider>> GetAuthenticationProviders();
    Task<List<IdentityProvider>> GetClientImportProviders();
    Task<TestConnectionResultResource> TestConnectionAsync(Guid providerId);
    Task<IdentityProviderSyncResultResource> SyncClientsAsync(Guid providerId);
}
