// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Domain.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Interfaces;

public interface IIdentityProviderRepository : IBaseRepository<IdentityProvider>
{
    Task<List<IdentityProvider>> GetEnabledProviders();
    Task<List<IdentityProvider>> GetAuthenticationProviders();
    Task<List<IdentityProvider>> GetClientImportProviders();
    Task<TestConnectionResultResource> TestConnectionAsync(Guid providerId);
    Task<IdentityProviderSyncResultResource> SyncClientsAsync(Guid providerId);
}
